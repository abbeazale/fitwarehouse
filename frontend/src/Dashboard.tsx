import { useState, useEffect, type ChangeEvent, type FormEvent } from 'react'
import { supabase } from './lib/supabase'
import Header from './components/Header'
import type { HealthRecord } from './types/Records'

const normalizeBaseUrl = (value: unknown, fallback: string) => {
  const url = typeof value === 'string' && value.trim().length > 0 ? value : fallback
  return url.replace(/\/$/, '')
}

const etlBaseUrl = normalizeBaseUrl(import.meta.env.VITE_ETL_BASE_URL, 'http://localhost:8000')

export default function Dashboard() {
  
  const [records, setRecords] = useState<HealthRecord[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [file, setFile] = useState<File | null>(null)
  const [uploading, setUploading] = useState(false)
  const [uploadMessage, setUploadMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null)
  const [dragActive, setDragActive] = useState(false)


  const loadRecords = async () => {
    try {
      const { data, error } = await supabase
        .from('inventory')
        .select('*')
        .order('processed_at_utc', { ascending: false })
        .limit(20)

      if (error) throw error

      const transformedRecords = (data || []).map((item) => ({
        id: item.id,
        productName: item.product_name,
        quantity: item.quantity,
        warehouseLocation: item.warehouse_location,
        submittedBy: item.submitted_by,
        processedAtUtc: item.processed_at_utc,
      }))

      setRecords(transformedRecords)
    } catch (error) {
      console.error('Failed to load records', error)
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    loadRecords()
  }, [])

 

  const handleDrag = (e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    if (e.type === 'dragenter' || e.type === 'dragover') {
      setDragActive(true)
    } else if (e.type === 'dragleave') {
      setDragActive(false)
    }
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    setDragActive(false)

    const droppedFile = e.dataTransfer.files?.[0]
    if (droppedFile) {
      validateAndSetFile(droppedFile)
    }
  }

  const validateAndSetFile = (selectedFile: File) => {
    const fileType = selectedFile.name.split('.').pop()?.toLowerCase()
    if (fileType === 'csv' || fileType === 'xml') {
      setFile(selectedFile)
      setUploadMessage(null)
    } else {
      setUploadMessage({ type: 'error', text: 'Please select a CSV or XML file' })
      setFile(null)
    }
  }

  const handleFileChange = (e: ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0]
    if (selectedFile) {
      validateAndSetFile(selectedFile)
    }
  }

  const handleUpload = async (e: FormEvent) => {
    e.preventDefault()
    if (!file) {
      setUploadMessage({ type: 'error', text: 'Please select a file' })
      return
    }

    setUploading(true)
    setUploadMessage(null)

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
      setUploadMessage({
        type: 'success',
        text: `Successfully processed ${result.recordsProcessed || 0} records`,
      })
      setFile(null)

      const fileInput = document.getElementById('file-input') as HTMLInputElement
      if (fileInput) fileInput.value = ''

      loadRecords()
    } catch (err) {
      setUploadMessage({
        type: 'error',
        text: err instanceof Error ? err.message : 'Upload failed',
      })
    } finally {
      setUploading(false)
    }
  }

  
  return (
    <div className="min-h-screen bg-slate-950">
      {/* Header */}
      <Header/>

      <main className="max-w-5xl mx-auto px-4 sm:px-8 py-8">
        {/* Header */}
        <header className="mb-8">
          <h1 className="text-3xl font-semibold text-slate-50 mb-2 tracking-tight">Health Data Dashboard</h1>
          <p className="text-slate-400">Upload and manage your health records</p>
        </header>

        {/* Upload Section */}
        <section className="bg-slate-900 border border-slate-800 rounded-2xl p-6 mb-8">
          <h2 className="text-xl font-semibold text-slate-50 mb-5">Upload Health Data File</h2>
          
          <form onSubmit={handleUpload}>
            <div
              className={`border-2 border-dashed rounded-xl p-8 text-center transition relative ${
                dragActive
                  ? 'border-teal-500 bg-teal-500/10'
                  : file
                  ? 'border-slate-700 bg-slate-800'
                  : 'border-slate-700 hover:border-teal-500 hover:bg-teal-500/5'
              }`}
              onDragEnter={handleDrag}
              onDragLeave={handleDrag}
              onDragOver={handleDrag}
              onDrop={handleDrop}
            >
              <input
                type="file"
                id="file-input"
                accept=".csv,.xml"
                onChange={handleFileChange}
                disabled={uploading}
                className="absolute inset-0 opacity-0 cursor-pointer"
              />

              {file ? (
                <div className="flex items-center gap-4 text-left">
                  <svg className="w-10 h-10 text-teal-500 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
                    <polyline points="14,2 14,8 20,8" />
                  </svg>
                  <div className="flex-1">
                    <span className="block font-medium text-slate-200">{file.name}</span>
                    <span className="text-sm text-slate-500">{(file.size / 1024).toFixed(2)} KB</span>
                  </div>
                  <button
                    type="button"
                    onClick={() => setFile(null)}
                    className="p-2 hover:bg-red-500/10 text-slate-400 hover:text-red-500 rounded-lg transition"
                    aria-label="Remove file"
                  >
                    <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                      <line x1="18" y1="6" x2="6" y2="18" />
                      <line x1="6" y1="6" x2="18" y2="18" />
                    </svg>
                  </button>
                </div>
              ) : (
                <label htmlFor="file-input" className="flex flex-col items-center gap-3 cursor-pointer">
                  <svg className="w-10 h-10 text-slate-500" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                    <polyline points="17,8 12,3 7,8" />
                    <line x1="12" y1="3" x2="12" y2="15" />
                  </svg>
                  <span className="text-slate-400">
                    Drag and drop your file here, or <strong className="text-teal-500">browse</strong>
                  </span>
                  <span className="text-sm text-slate-500">Supports CSV and XML files</span>
                </label>
              )}
            </div>

            {uploadMessage && (
              <div className={`mt-4 flex items-center gap-2 px-4 py-3 rounded-lg text-sm ${
                uploadMessage.type === 'success'
                  ? 'bg-green-500/15 text-green-500'
                  : 'bg-red-500/15 text-red-500'
              }`}>
                {uploadMessage.type === 'success' ? (
                  <svg className="w-5 h-5 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
                    <polyline points="22,4 12,14.01 9,11.01" />
                  </svg>
                ) : (
                  <svg className="w-5 h-5 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <circle cx="12" cy="12" r="10" />
                    <line x1="15" y1="9" x2="9" y2="15" />
                    <line x1="9" y1="9" x2="15" y2="15" />
                  </svg>
                )}
                {uploadMessage.text}
              </div>
            )}

            <button
              type="submit"
              disabled={!file || uploading}
              className="mt-4 w-full flex items-center justify-center gap-2 bg-teal-500 hover:bg-teal-600 text-white font-medium py-3 rounded-xl transition-all hover:shadow-lg hover:shadow-teal-500/30 hover:-translate-y-0.5 disabled:opacity-60 disabled:cursor-not-allowed disabled:hover:translate-y-0"
            >
              {uploading ? (
                <>
                  <svg className="w-5 h-5 animate-spin" viewBox="0 0 24 24" fill="none">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                  </svg>
                  Uploading...
                </>
              ) : (
                <>
                  <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                    <polyline points="17,8 12,3 7,8" />
                    <line x1="12" y1="3" x2="12" y2="15" />
                  </svg>
                  Upload File
                </>
              )}
            </button>
          </form>
        </section>

        {/* Records Section */}
        <section className="bg-slate-900 border border-slate-800 rounded-2xl p-6">
          <h2 className="text-xl font-semibold text-slate-50 mb-5">Recent Records</h2>

          {isLoading ? (
            <div className="flex flex-col items-center justify-center py-12">
              <div className="w-10 h-10 border-3 border-slate-700 border-t-teal-500 rounded-full animate-spin mb-4" />
              <p className="text-slate-400">Loading records...</p>
            </div>
          ) : records.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 text-slate-400">
              <svg className="w-12 h-12 text-slate-600 mb-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
                <polyline points="14,2 14,8 20,8" />
                <line x1="16" y1="13" x2="8" y2="13" />
                <line x1="16" y1="17" x2="8" y2="17" />
                <polyline points="10,9 9,9 8,9" />
              </svg>
              <p className="font-medium text-slate-300 mb-1">No records yet</p>
              <span className="text-sm">Upload a file to get started</span>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-slate-800">
                    <th className="text-left py-3 px-4 text-xs font-semibold text-slate-500 uppercase tracking-wider">Product</th>
                    <th className="text-left py-3 px-4 text-xs font-semibold text-slate-500 uppercase tracking-wider">Quantity</th>
                    <th className="text-left py-3 px-4 text-xs font-semibold text-slate-500 uppercase tracking-wider">Location</th>
                    <th className="text-left py-3 px-4 text-xs font-semibold text-slate-500 uppercase tracking-wider">Submitted By</th>
                    <th className="text-left py-3 px-4 text-xs font-semibold text-slate-500 uppercase tracking-wider">Date</th>
                  </tr>
                </thead>
                <tbody>
                  {records.map((record) => (
                    <tr key={record.id} className="border-b border-slate-800 last:border-0 hover:bg-slate-800/50 transition">
                      <td className="py-3.5 px-4 text-slate-200">{record.productName}</td>
                      <td className="py-3.5 px-4">
                        <span className="inline-block px-3 py-1 bg-teal-500/15 text-teal-500 rounded-lg text-sm font-medium">
                          {record.quantity}
                        </span>
                      </td>
                      <td className="py-3.5 px-4 text-slate-200">{record.warehouseLocation}</td>
                      <td className="py-3.5 px-4 text-slate-200">{record.submittedBy}</td>
                      <td className="py-3.5 px-4 text-slate-400">{new Date(record.processedAtUtc).toLocaleDateString()}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </main>
    </div>
  )
}
