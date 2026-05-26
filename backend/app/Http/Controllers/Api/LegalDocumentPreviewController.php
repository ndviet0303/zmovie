<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\LegalDocument;
use Illuminate\Support\Facades\Storage;

class LegalDocumentPreviewController extends Controller
{
    public function show(LegalDocument $legalDocument)
    {
        if (preg_match('/^https?:\/\//i', $legalDocument->path)) {
            return redirect()->away($legalDocument->path);
        }

        $disk = $legalDocument->disk ?: 'private';
        $path = ltrim($legalDocument->path, '/');

        if (! Storage::disk($disk)->exists($path)) {
            return response($this->fallbackPdf($legalDocument), 200, [
                'Content-Type' => 'application/pdf',
                'Content-Disposition' => 'inline; filename="'.($legalDocument->original_filename ?: 'demo-legal-document.pdf').'"',
                'Cache-Control' => 'private, max-age=60',
            ]);
        }

        $absolutePath = Storage::disk($disk)->path($path);
        $filename = $legalDocument->original_filename ?: basename($path);

        return response()->file($absolutePath, [
            'Content-Type' => $legalDocument->mime_type ?: 'application/octet-stream',
            'Content-Disposition' => 'inline; filename="'.$filename.'"',
            'Cache-Control' => 'private, max-age=300',
        ]);
    }

    private function fallbackPdf(LegalDocument $document): string
    {
        return $this->demoPdf([
            'ZMovie legal document fallback',
            "Title: {$document->title}",
            "Type: {$document->document_type}",
            "Status: {$document->status}",
            "Missing storage path: {$document->path}",
        ]);
    }

    private function pdfText(string $value): string
    {
        return str_replace(['\\', '(', ')'], ['\\\\', '\\(', '\\)'], str()->ascii($value));
    }

    private function demoPdf(array $lines): string
    {
        $text = "BT /F1 16 Tf 72 720 Td ";

        foreach ($lines as $index => $line) {
            if ($index > 0) {
                $text .= '0 -28 Td ';
            }

            $text .= "({$this->pdfText($line)}) Tj ";
        }

        $text .= "ET\n";

        $objects = [
            '<< /Type /Catalog /Pages 2 0 R >>',
            '<< /Type /Pages /Kids [3 0 R] /Count 1 >>',
            '<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> >> >> /Contents 4 0 R >>',
            "<< /Length ".strlen($text)." >>\nstream\n{$text}endstream",
        ];

        $pdf = "%PDF-1.4\n";
        $offsets = [0];

        foreach ($objects as $number => $object) {
            $offsets[] = strlen($pdf);
            $pdf .= ($number + 1)." 0 obj\n{$object}\nendobj\n";
        }

        $xrefOffset = strlen($pdf);
        $pdf .= "xref\n0 ".(count($objects) + 1)."\n";
        $pdf .= "0000000000 65535 f \n";

        foreach (array_slice($offsets, 1) as $offset) {
            $pdf .= str_pad((string) $offset, 10, '0', STR_PAD_LEFT)." 00000 n \n";
        }

        $pdf .= "trailer << /Size ".(count($objects) + 1)." /Root 1 0 R >>\n";
        $pdf .= "startxref\n{$xrefOffset}\n%%EOF\n";

        return $pdf;
    }
}
