import { useState, type ChangeEvent, type FormEvent } from 'react'

type FileUploadProps = {
  onUploadSuccess: () => void
  etlBaseUrl: string
}

export default function FileUpload({ onUploadSuccess, etlBaseUrl }: FileUploadProps) {
  const [file, setFile] = useState<File | null>(null)
  const [uploading, setUploading] = useState(false)
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const handleFileChange = (e: ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0]
    if (selectedFile) {
      const fileType = selectedFile.name.split('.').pop()?.toLowerCase()
      if (fileType === 'csv' || fileType === 'xml') {
        setFile(selectedFile)
        setError(null)
      } else {
        setError('Please select a CSV or XML file')
        setFile(null)
      }
    }
  }

  const handleUpload = async (e: FormEvent) => {
    e.preventDefault()
    if (!file) {
      setError('Please select a file')
      return
    }

    setUploading(true)
    setError(null)
    setMessage(null)

    try {
      const formData = new FormData()
      formData.append('file', file)

      const response = await fetch(`${etlBaseUrl}/upload`, {
        method: 'POST',
        body: formData,
      })

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}))
        throw new Error(errorData?.detail || 'Upload failed')
      }

      const result = await response.json()
      setMessage(`Successfully uploaded ${result.recordsProcessed || 0} records!`)
      setFile(null)
      
      // Reset file input
      const fileInput = document.getElementById('file-input') as HTMLInputElement
      if (fileInput) fileInput.value = ''

      // Notify parent to refresh data
      onUploadSuccess()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Upload failed')
    } finally {
      setUploading(false)
    }
  }

  return (
    <div className="border rounded-2xl p-6">
      <h3 className="text-lg font-semibold mb-4">Upload Inventory File</h3>
      <form onSubmit={handleUpload} className="flex flex-col gap-4">
        <div className="flex flex-col gap-2">
          <label htmlFor="file-input" className="text-sm font-medium">
            Select CSV or XML file:
          </label>
          <input
            id="file-input"
            type="file"
            accept=".csv,.xml"
            onChange={handleFileChange}
            className="border rounded-lg px-3 py-2"
            disabled={uploading}
          />
        </div>

        {file && (
          <div className="text-sm text-gray-600">
            Selected: <span className="font-medium">{file.name}</span> (
            {(file.size / 1024).toFixed(2)} KB)
          </div>
        )}

        {error && <p className="text-red-600 text-sm">{error}</p>}
        {message && <p className="text-green-600 text-sm">{message}</p>}

        <button
          type="submit"
          disabled={!file || uploading}
          className="border rounded-2xl py-2 px-4 hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {uploading ? 'Uploading...' : 'Upload File'}
        </button>
      </form>
    </div>
  )
}

