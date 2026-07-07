import { useId, useRef, useState } from 'react'
import { ImagePlus, Upload, X } from 'lucide-react'
import { cn } from '@/lib/utils'

interface FileUploadProps {
  accept?: string
  hint?: string
  previewUrl?: string | null
  onChange: (file: File | null) => void
  className?: string
  disabled?: boolean
}

export function FileUpload({
  accept = 'image/*',
  hint = 'PNG, JPG or SVG · max 5 MB',
  previewUrl,
  onChange,
  className,
  disabled,
}: FileUploadProps) {
  const inputId = useId()
  const inputRef = useRef<HTMLInputElement>(null)
  const [dragOver, setDragOver] = useState(false)
  const [localPreview, setLocalPreview] = useState<string | null>(null)
  const [fileName, setFileName] = useState<string | null>(null)

  const displayPreview = localPreview ?? previewUrl ?? null
  const isImage = accept.includes('image')

  function pickFile(file: File | null) {
    if (!file) {
      setLocalPreview(null)
      setFileName(null)
      onChange(null)
      if (inputRef.current) inputRef.current.value = ''
      return
    }
    setFileName(file.name)
    if (file.type.startsWith('image/')) {
      setLocalPreview(URL.createObjectURL(file))
    } else {
      setLocalPreview(null)
    }
    onChange(file)
  }

  function handleDrop(e: React.DragEvent) {
    e.preventDefault()
    setDragOver(false)
    if (disabled) return
    const file = e.dataTransfer.files?.[0]
    if (file) pickFile(file)
  }

  return (
    <div className={cn('space-y-2', className)}>
      <div
        role="button"
        tabIndex={disabled ? -1 : 0}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') inputRef.current?.click()
        }}
        onClick={() => !disabled && inputRef.current?.click()}
        onDragOver={(e) => {
          e.preventDefault()
          if (!disabled) setDragOver(true)
        }}
        onDragLeave={() => setDragOver(false)}
        onDrop={handleDrop}
        className={cn(
          'group relative flex cursor-pointer flex-col items-center justify-center gap-3 rounded-2xl border-2 border-dashed px-6 py-8 text-center transition-all',
          dragOver
            ? 'border-[#2081A1] bg-[#2081A1]/8 shadow-[0_0_0_4px_rgba(32,129,161,0.12)]'
            : 'border-slate-200 bg-gradient-to-br from-slate-50 to-white hover:border-[#2081A1]/50 hover:bg-[#2081A1]/5',
          disabled && 'cursor-not-allowed opacity-60',
        )}
      >
        <input
          ref={inputRef}
          id={inputId}
          type="file"
          accept={accept}
          disabled={disabled}
          className="sr-only"
          onChange={(e) => pickFile(e.target.files?.[0] ?? null)}
        />

        {displayPreview && isImage ? (
          <div className="relative">
            <img
              src={displayPreview}
              alt="Preview"
              className="h-20 w-20 rounded-xl border border-white object-contain shadow-md ring-1 ring-slate-200"
            />
            {!disabled && (
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation()
                  pickFile(null)
                }}
                className="absolute -right-2 -top-2 rounded-full bg-white p-1 text-slate-500 shadow ring-1 ring-slate-200 hover:text-red-600"
              >
                <X className="h-3.5 w-3.5" />
              </button>
            )}
          </div>
        ) : (
          <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-[#2081A1]/10 text-[#2081A1] transition group-hover:scale-105 group-hover:bg-[#2081A1]/15">
            {isImage ? <ImagePlus className="h-7 w-7" /> : <Upload className="h-7 w-7" />}
          </div>
        )}

        <div>
          <p className="text-sm font-semibold text-slate-800">
            {fileName ? fileName : 'Drop your file here, or browse'}
          </p>
          <p className="mt-1 text-xs text-slate-500">{hint}</p>
        </div>

        <span className="inline-flex items-center gap-1.5 rounded-full bg-[#2081A1] px-4 py-1.5 text-xs font-semibold text-white shadow-sm transition group-hover:bg-[#1a6d89]">
          <Upload className="h-3.5 w-3.5" />
          Choose file
        </span>
      </div>
    </div>
  )
}
