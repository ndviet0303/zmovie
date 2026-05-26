<?php

return [
    'ffmpeg_binary' => env('FFMPEG_BINARY', 'ffmpeg'),
    'timeout' => (int) env('TRANSCODE_TIMEOUT', 3600),
    'hls_time' => (int) env('TRANSCODE_HLS_TIME', 6),
    'output_disk' => env('TRANSCODE_OUTPUT_DISK', 'public'),
    'output_path' => env('TRANSCODE_OUTPUT_PATH', 'hls'),

    'demo_quota' => [
        'enabled' => env('DEMO_VIDEO_STORAGE_QUOTA_ENABLED', true),
        'limit_bytes' => (int) env('DEMO_VIDEO_STORAGE_QUOTA_BYTES', 1073741824),
        'transcode_reserve_multiplier' => (float) env('DEMO_VIDEO_TRANSCODE_RESERVE_MULTIPLIER', 2.5),
        'paths' => array_filter(array_map(
            'trim',
            explode(',', env('DEMO_VIDEO_STORAGE_QUOTA_PATHS', 'private:uploads,public:hls')),
        )),
    ],

    'renditions' => [
        [
            'name' => '360p',
            'height' => 360,
            'bandwidth' => 800000,
            'video_bitrate' => '800k',
            'audio_bitrate' => '96k',
            'crf' => 23,
        ],
        [
            'name' => '720p',
            'height' => 720,
            'bandwidth' => 2800000,
            'video_bitrate' => '2800k',
            'audio_bitrate' => '128k',
            'crf' => 21,
        ],
    ],
];
