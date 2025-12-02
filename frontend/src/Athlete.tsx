import { useEffect, useMemo, useState } from 'react'
import Header from './components/Header'
import { Line } from 'react-chartjs-2'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Tooltip,
  Legend,
} from 'chart.js'

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Tooltip, Legend)

type AthletePacePoint = {
  weekStart: string
  paceMinPerKm: number
}

type AthletePaceResponse = {
  athleteId: number
  weeks: number
  series: AthletePacePoint[]
  majors: AthleteMajor[]
}

type AthleteMajor = {
  majorName: string
  majorYear: number | null
}

const normalizeBaseUrl = (value: unknown, fallback: string) => {
  const url = typeof value === 'string' && value.trim().length > 0 ? value : fallback
  return url.replace(/\/$/, '')
}

const rawApiBase =
  import.meta.env.VITE_BACKEND_BASE_URL ?? import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5018'
const apiBaseUrl = normalizeBaseUrl(rawApiBase, 'http://localhost:5018')

export default function Athlete() {
  const [athleteId, setAthleteId] = useState<string>('1')
  const [data, setData] = useState<AthletePaceResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const fetchData = (id: number) => {
    setLoading(true)
    setError(null)
    const controller = new AbortController()

    fetch(`${apiBaseUrl}/api/analytics/athlete-pace?athleteId=${id}&weeks=52`, { signal: controller.signal })
      .then(async (res) => {
        if (!res.ok) {
          const txt = await res.text()
          throw new Error(txt || `HTTP ${res.status}`)
        }
        return res.json() as Promise<AthletePaceResponse>
      })
      .then(setData)
      .catch((err) => {
        if (err.name !== 'AbortError') setError('Failed to load athlete pace')
      })
      .finally(() => setLoading(false))

    return () => controller.abort()
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    const idNum = Number(athleteId)
    if (!idNum || idNum <= 0) {
      setError('Please enter a valid athlete ID')
      return
    }
    fetchData(idNum)
  }

  useEffect(() => {
    const idNum = Number(athleteId)
    if (idNum > 0) {
      fetchData(idNum)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const chartData = useMemo(() => {
    if (!data) return null
    const labels = data.series.map((pt) => new Date(pt.weekStart))
    const dataset = data.series.map((pt) => pt.paceMinPerKm)
    return { labels, dataset }
  }, [data])

  return (
    <div className="min-h-screen bg-slate-950 text-slate-50">
      <Header />

      <main className="max-w-5xl mx-auto px-4 sm:px-8 py-8 space-y-6">
        <div>
          <h1 className="text-3xl font-semibold tracking-tight mb-2">Athlete Pace</h1>
          <p className="text-slate-400">Enter an athlete ID to see pace improvement over the last year.</p>
        </div>

        <form onSubmit={handleSubmit} className="flex items-center gap-3">
          <input
            type="number"
            min="1"
            value={athleteId}
            onChange={(e) => setAthleteId(e.target.value)}
            className="px-3 py-2 rounded-lg bg-slate-900 border border-slate-800 text-slate-100 focus:border-teal-500 focus:outline-none"
            placeholder="Athlete ID"
          />
          <button
            type="submit"
            className="px-4 py-2 rounded-lg bg-teal-500 text-slate-950 font-medium hover:bg-teal-400 transition"
            disabled={loading}
          >
            {loading ? 'Loading...' : 'Load'}
          </button>
          {error && <span className="text-sm text-red-400">{error}</span>}
        </form>

        <section className="bg-slate-900 border border-slate-800 rounded-2xl p-6">
          <div className="flex items-center justify-between mb-4">
            <div>
              <h2 className="text-xl font-semibold">Pace (min/km) over time</h2>
              <p className="text-slate-500 text-sm">Lower is faster. Latest 52 weeks.</p>
            </div>
          </div>
          {!data || !chartData ? (
            <div className="text-slate-500">No data</div>
          ) : chartData.dataset.length === 0 ? (
            <div className="text-slate-500">No pace records for this athlete.</div>
          ) : (
            <LineChart
              height={360}
              labels={chartData.labels}
              values={chartData.dataset}
              label={`Athlete ${data.athleteId}`}
            />
          )}
        </section>

        <section className="bg-slate-900 border border-slate-800 rounded-2xl p-6">
          <div className="mb-3">
            <h2 className="text-lg font-semibold">Majors</h2>
            <p className="text-slate-500 text-sm">Events this athlete has participated in.</p>
          </div>
          {!data || data.majors.length === 0 ? (
            <div className="text-slate-500">No majors found for this athlete.</div>
          ) : (
            <div className="flex flex-wrap gap-2">
              {data.majors.map((m, idx) => (
                <span
                  key={`${m.majorName}-${idx}`}
                  className="px-3 py-1 rounded-full border border-slate-700 bg-slate-800 text-sm text-slate-200"
                >
                  {m.majorName}
                  {m.majorYear ? ` (${m.majorYear})` : ''}
                </span>
              ))}
            </div>
          )}
        </section>
      </main>
    </div>
  )
}

type LineChartProps = {
  height: number
  labels: Date[]
  values: number[]
  label: string
}

function LineChart({ height, labels, values, label }: LineChartProps) {
  const data = {
    labels: labels.map((d) => d.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })),
    datasets: [
      {
        label,
        data: values,
        borderColor: '#10b981',
        backgroundColor: '#10b981',
        tension: 0.2,
        spanGaps: true,
        pointRadius: 3,
      },
    ],
  }

  const options = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        labels: { color: '#cbd5e1' },
      },
      tooltip: {
        callbacks: {
          label: (ctx: any) => `${ctx.dataset.label}: ${Number(ctx.raw ?? 0).toFixed(2)} min/km`,
        },
      },
    },
    scales: {
      x: {
        ticks: { color: '#94a3b8' },
        grid: { color: '#1e293b' },
      },
      y: {
        ticks: {
          color: '#94a3b8',
          callback: (val: any) => Number(val).toFixed(2),
        },
        grid: { color: '#1e293b' },
        beginAtZero: false,
      },
    },
  }

  return (
    <div className="relative" style={{ height }}>
      <Line data={data} options={options} />
    </div>
  )
}
