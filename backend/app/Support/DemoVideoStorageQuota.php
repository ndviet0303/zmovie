<?php

namespace App\Support;

use Illuminate\Support\Facades\Storage;
use Symfony\Component\HttpKernel\Exception\HttpException;

class DemoVideoStorageQuota
{
    public function assertHasSpace(int $incomingBytes = 0): void
    {
        if (! config('transcoding.demo_quota.enabled', true)) {
            return;
        }

        $limit = (int) config('transcoding.demo_quota.limit_bytes', 1073741824);
        $used = $this->usedBytes();

        if (($used + $incomingBytes) > $limit) {
            throw new HttpException(
                507,
                'diskfull: demo video storage quota exceeded. Limit is '.$this->humanBytes($limit).', current usage is '.$this->humanBytes($used).'.',
            );
        }
    }

    public function usedBytes(): int
    {
        return collect(config('transcoding.demo_quota.paths', []))
            ->map(fn (string $path) => $this->pathBytes($path))
            ->sum();
    }

    private function pathBytes(string $configuredPath): int
    {
        [$diskName, $path] = array_pad(explode(':', $configuredPath, 2), 2, '');
        $disk = Storage::disk($diskName ?: config('filesystems.default'));
        $root = $disk->path(trim($path, '/'));

        if (! is_dir($root)) {
            return 0;
        }

        $bytes = 0;
        $iterator = new \RecursiveIteratorIterator(
            new \RecursiveDirectoryIterator($root, \FilesystemIterator::SKIP_DOTS),
        );

        foreach ($iterator as $file) {
            if ($file->isFile()) {
                $bytes += $file->getSize();
            }
        }

        return $bytes;
    }

    private function humanBytes(int $bytes): string
    {
        if ($bytes >= 1073741824) {
            return round($bytes / 1073741824, 2).'GB';
        }

        if ($bytes >= 1048576) {
            return round($bytes / 1048576, 2).'MB';
        }

        return $bytes.'B';
    }
}
