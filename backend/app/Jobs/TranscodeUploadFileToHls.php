<?php

namespace App\Jobs;

use App\Models\UploadFile;
use App\Models\VideoSource;
use App\Support\DemoVideoStorageQuota;
use Illuminate\Bus\Queueable;
use Illuminate\Contracts\Queue\ShouldQueue;
use Illuminate\Foundation\Bus\Dispatchable;
use Illuminate\Queue\InteractsWithQueue;
use Illuminate\Queue\SerializesModels;
use Illuminate\Support\Facades\Storage;
use Illuminate\Support\Str;
use RuntimeException;
use Symfony\Component\Process\Process;
use Throwable;

class TranscodeUploadFileToHls implements ShouldQueue
{
    use Dispatchable, InteractsWithQueue, Queueable, SerializesModels;

    public int $timeout = 3600;

    public function __construct(public int $uploadFileId)
    {
        $this->onQueue('transcoding');
        $this->timeout = (int) config('transcoding.timeout', 3600);
    }

    public function handle(DemoVideoStorageQuota $quota): void
    {
        $uploadFile = UploadFile::query()
            ->with(['movieUpload.movie', 'movieUpload.files'])
            ->findOrFail($this->uploadFileId);

        $upload = $uploadFile->movieUpload;
        $movie = $upload->movie;

        if (! $movie) {
            throw new RuntimeException('Cannot transcode upload without a target movie.');
        }

        $inputPath = $this->absoluteInputPath($uploadFile);
        $inputSize = filesize($inputPath) ?: (int) $uploadFile->file_size_bytes;
        $quota->assertHasSpace((int) ($inputSize * (float) config('transcoding.demo_quota.transcode_reserve_multiplier', 2.5)));

        $outputDisk = config('transcoding.output_disk', 'public');
        $outputDirectory = trim(config('transcoding.output_path', 'hls'), '/')."/movies/{$movie->id}/uploads/{$upload->id}";
        $absoluteOutputDirectory = Storage::disk($outputDisk)->path($outputDirectory);

        if (! is_dir($absoluteOutputDirectory)) {
            mkdir($absoluteOutputDirectory, 0755, true);
        }

        $upload->update(['status' => 'transcoding']);
        $uploadFile->update([
            'status' => 'processing',
            'processing_job_id' => $this->job?->getJobId(),
            'failure_reason' => null,
        ]);

        $renditions = config('transcoding.renditions', []);

        foreach ($renditions as $rendition) {
            $this->runFfmpeg($inputPath, $absoluteOutputDirectory, $rendition);
        }

        file_put_contents(
            "{$absoluteOutputDirectory}/master.m3u8",
            $this->masterPlaylist($renditions),
        );

        VideoSource::query()
            ->where('movie_id', $movie->id)
            ->where('is_default', true)
            ->update(['is_default' => false]);

        VideoSource::updateOrCreate(
            [
                'movie_id' => $movie->id,
                'label' => 'Transcoded HLS',
            ],
            [
                'source_type' => 'hls',
                'quality' => 'auto',
                'url' => "{$outputDirectory}/master.m3u8",
                'cdn_provider' => 'local',
                'duration_seconds' => $uploadFile->duration_seconds,
                'is_default' => true,
                'is_active' => true,
            ],
        );

        $uploadFile->update([
            'status' => 'ready',
            'technical_metadata' => [
                ...($uploadFile->technical_metadata ?? []),
                'transcoded_to' => "{$outputDirectory}/master.m3u8",
                'renditions' => collect($renditions)->pluck('name')->all(),
            ],
        ]);

        $upload->update([
            'status' => 'approved',
            'metadata' => [
                ...($upload->metadata ?? []),
                'transcoded_video_source' => "{$outputDirectory}/master.m3u8",
            ],
        ]);
    }

    public function failed(Throwable $exception): void
    {
        $uploadFile = UploadFile::query()->with('movieUpload')->find($this->uploadFileId);

        if (! $uploadFile) {
            return;
        }

        $uploadFile->update([
            'status' => 'failed',
            'failure_reason' => Str::limit($exception->getMessage(), 2000),
        ]);

        $uploadFile->movieUpload?->update([
            'status' => 'rejected',
            'rejection_reason' => 'Transcode failed: '.Str::limit($exception->getMessage(), 1000),
        ]);
    }

    private function absoluteInputPath(UploadFile $uploadFile): string
    {
        $disk = Storage::disk($uploadFile->disk ?: 'private');

        if (! $disk->exists($uploadFile->path)) {
            throw new RuntimeException("Input video not found on disk [{$uploadFile->disk}]: {$uploadFile->path}");
        }

        return $disk->path($uploadFile->path);
    }

    /**
     * @param  array{name:string,height:int,video_bitrate:string,audio_bitrate:string,crf:int}  $rendition
     */
    private function runFfmpeg(string $inputPath, string $outputDirectory, array $rendition): void
    {
        $name = $rendition['name'];
        $playlistPath = "{$name}.m3u8";
        $segmentPattern = "{$name}_%05d.ts";

        $process = new Process([
            config('transcoding.ffmpeg_binary', 'ffmpeg'),
            '-y',
            '-i', $inputPath,
            '-vf', "scale=-2:{$rendition['height']}",
            '-c:v', 'libx264',
            '-preset', 'veryfast',
            '-crf', (string) $rendition['crf'],
            '-maxrate', $rendition['video_bitrate'],
            '-bufsize', $this->doubleBitrate($rendition['video_bitrate']),
            '-c:a', 'aac',
            '-b:a', $rendition['audio_bitrate'],
            '-ac', '2',
            '-f', 'hls',
            '-hls_time', (string) config('transcoding.hls_time', 6),
            '-hls_playlist_type', 'vod',
            '-hls_segment_filename', $segmentPattern,
            $playlistPath,
        ]);

        $process->setWorkingDirectory($outputDirectory);
        $process->setTimeout((int) config('transcoding.timeout', 3600));
        $process->run();

        if (! $process->isSuccessful()) {
            throw new RuntimeException(trim($process->getErrorOutput() ?: $process->getOutput()));
        }
    }

    /**
     * @param  array<int, array{name:string,height:int,bandwidth:int}>  $renditions
     */
    private function masterPlaylist(array $renditions): string
    {
        $lines = ['#EXTM3U', '#EXT-X-VERSION:3'];

        foreach ($renditions as $rendition) {
            $width = $rendition['height'] === 360 ? 640 : 1280;
            $lines[] = "#EXT-X-STREAM-INF:BANDWIDTH={$rendition['bandwidth']},RESOLUTION={$width}x{$rendition['height']}";
            $lines[] = "{$rendition['name']}.m3u8";
        }

        return implode("\n", $lines)."\n";
    }

    private function doubleBitrate(string $bitrate): string
    {
        if (preg_match('/^(\d+)k$/', $bitrate, $matches)) {
            return ((int) $matches[1] * 2).'k';
        }

        return $bitrate;
    }
}
