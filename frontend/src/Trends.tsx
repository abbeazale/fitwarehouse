import { useEffect, useMemo, useState } from 'react'
import { Line } from 'react-chartjs-2'
import Header from './components/Header'
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

type PaceSeriesPoint = {
  weekStart: string
  label: string
  avgPaceMinPerKm: number
}

type PaceResponse = {
  weeks: number
  series: PaceSeriesPoint[]
}

type MajorDistancePoint = {
  weekStart: string
  majorYear: string
  totalDistanceKm: number
  runnerCount: number
  avgDistanceKmPerRunner: number
}

type MajorDistanceResponse = {
  weeks: number
  series: MajorDistancePoint[]
}

type GenderDistancePoint = {
  weekStart: string
  gender: string
  totalDistanceKm: number
  runnerCount: number
  avgDistanceKmPerRunner: number
}

type GenderDistanceResponse = {
  weeks: number
  series: GenderDistancePoint[]
}

const normalizeBaseUrl = (value: unknown, fallback: string) => {
  const url = typeof value === 'string' && value.trim().length > 0 ? value : fallback
  return url.replace(/\/$/, '')
}

const rawApiBase =
  import.meta.env.VITE_BACKEND_BASE_URL ?? import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5018'
const apiBaseUrl = normalizeBaseUrl(rawApiBase, 'http://localhost:5018')

export default function Trends() {
  const [data, setData] = useState<PaceResponse | null>(null)
  const [majors, setMajors] = useState<MajorDistanceResponse | null>(null)
  const [genderDistance, setGenderDistance] = useState<GenderDistanceResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    setLoading(true)
    setError(null)

    fetch(`${apiBaseUrl}/api/analytics/pace-by-demo?weeks=12`, { signal: controller.signal })
      .then(async (res) => {
        if (!res.ok) {
          const txt = await res.text()
          throw new Error(txt || `HTTP ${res.status}`)
        }
        return res.json() as Promise<PaceResponse>
      })
      .then(setData)
      .then(() =>
        fetch(`${apiBaseUrl}/api/analytics/major-distance-by-year?weeks=12`, { signal: controller.signal })
          .then(async (res) => {
            if (!res.ok) {
              const txt = await res.text()
              throw new Error(txt || `HTTP ${res.status}`)
            }
            return res.json() as Promise<MajorDistanceResponse>
          })
          .then(setMajors)
          .then(() =>
            fetch(`${apiBaseUrl}/api/analytics/distance-by-gender?weeks=52`, { signal: controller.signal })
              .then(async (res) => {
                if (!res.ok) {
                  const txt = await res.text()
                  throw new Error(txt || `HTTP ${res.status}`)
                }
                return res.json() as Promise<GenderDistanceResponse>
              })
              .then(setGenderDistance)
          )
      )
      .catch((err) => {
        if (err.name !== 'AbortError') setError('Failed to load trends')
      })
      .finally(() => setLoading(false))

    return () => controller.abort()
  }, [])

  const grouped = useMemo(() => {
    if (!data) return {}
    const map: Record<string, { week: Date; value: number }[]> = {}
    data.series.forEach((pt) => {
      const key = pt.label || 'Unknown'
      if (!map[key]) map[key] = []
      map[key].push({ week: new Date(pt.weekStart), value: pt.avgPaceMinPerKm })
    })
    Object.values(map).forEach((arr) => arr.sort((a, b) => a.week.getTime() - b.week.getTime()))
    return map
  }, [data])

  const weeks = useMemo(() => {
    if (!data) return []
    return Array.from(new Set(data.series.map((s) => s.weekStart))).sort()
  }, [data])

  const majorsGrouped = useMemo(() => {
    if (!majors) return {}
    const map: Record<string, { week: Date; value: number }[]> = {}
    majors.series.forEach((pt) => {
      const key = pt.majorYear || 'Unknown'
      if (!map[key]) map[key] = []
      if (pt.avgDistanceKmPerRunner > 0) {
        map[key].push({ week: new Date(pt.weekStart), value: pt.avgDistanceKmPerRunner })
      }
    })
    Object.keys(map).forEach((k) => {
      if (map[k].length === 0) {
        delete map[k]
      } else {
        map[k].sort((a, b) => a.week.getTime() - b.week.getTime())
      }
    })
    return map
  }, [majors])

  const majorWeeks = useMemo(() => {
    if (!majors) return []
    return Array.from(new Set(majors.series.map((s) => s.weekStart))).sort()
  }, [majors])

  const genderGrouped = useMemo(() => {
    if (!genderDistance) return {}
    const map: Record<string, { week: Date; value: number }[]> = {}
    genderDistance.series.forEach((pt) => {
      const key = pt.gender || 'U'
      if (!map[key]) map[key] = []
      map[key].push({ week: new Date(pt.weekStart), value: pt.avgDistanceKmPerRunner })
    })
    Object.values(map).forEach((arr) => arr.sort((a, b) => a.week.getTime() - b.week.getTime()))
    return map
  }, [genderDistance])

  const genderWeeks = useMemo(() => {
    if (!genderDistance) return []
    return Array.from(new Set(genderDistance.series.map((s) => s.weekStart))).sort()
  }, [genderDistance])

  const colors = ['#10b981', '#22d3ee', '#a855f7', '#f97316', '#38bdf8', '#f43f5e', '#c084fc', '#fbbf24']
  const labels = Object.keys(grouped)

  return (
    <div className="min-h-screen bg-slate-950 text-slate-50">
      <Header />

      <main className="max-w-6xl mx-auto px-4 sm:px-8 py-8 space-y-8">
        {loading ? (
          <div className="flex items-center justify-center py-16 text-slate-400">Loading...</div>
        ) : error ? (
          <div className="bg-red-500/10 border border-red-500/30 text-red-200 px-4 py-3 rounded-lg">{error}</div>
        ) : data ? (
          <>
            <section className="bg-slate-900 border border-slate-800 rounded-2xl p-6">
              <div className="flex items-center justify-between mb-4">
                <div>
                  <h2 className="text-xl font-semibold">Pace (min/km) over time</h2>
                  <p className="text-slate-500 text-sm">Lower is faster. Grouped by gender + age group.</p>
                </div>
              </div>
              {labels.length === 0 ? (
                <div className="text-slate-500">No data</div>
              ) : (
                <LineChart height={360} series={grouped} colors={colors} weeks={weeks} valueLabel="min/km" />
              )}
            </section>

            <section className="bg-slate-900 border border-slate-800 rounded-2xl p-6">
              <div className="flex items-center justify-between mb-4">
                <div>
                  <h2 className="text-xl font-semibold">Distance by major year</h2>
                  <p className="text-slate-500 text-sm">Total weekly distance grouped by major year.</p>
                </div>
              </div>
              {majorsGrouped && Object.keys(majorsGrouped).length ? (
                <LineChart height={360} series={majorsGrouped} colors={colors} weeks={majorWeeks} valueLabel="km" />
              ) : (
                <div className="text-slate-500">No data</div>
              )}
            </section>

            <section className="bg-slate-900 border border-slate-800 rounded-2xl p-6">
              <div className="flex items-center justify-between mb-4">
                <div>
                  <h2 className="text-xl font-semibold">Distance by gender</h2>
                  <p className="text-slate-500 text-sm">Average weekly distance per runner by gender (last 52 weeks).</p>
                </div>
              </div>
              {genderGrouped && Object.keys(genderGrouped).length ? (
                <LineChart height={360} series={genderGrouped} colors={colors} weeks={genderWeeks} valueLabel="km" />
              ) : (
                <div className="text-slate-500">No data</div>
              )}
            </section>

          </>
        ) : null}
      </main>
    </div>
  )
}

type LineChartProps = {
  height: number
  series: Record<string, { week: Date; value: number }[]>
  colors: string[]
  weeks: string[]
  valueLabel?: string
}

function LineChart({ height, series, colors, weeks, valueLabel }: LineChartProps) {
  const labels = weeks.map((w) => new Date(w)).sort((a, b) => a.getTime() - b.getTime())

  const datasets = Object.entries(series).map(([label, points], idx) => {
    const map = new Map(points.map((p) => [p.week.toDateString(), p.value]))
    return {
      label,
      data: labels.map((d) => map.get(d.toDateString()) ?? null),
      borderColor: colors[idx % colors.length],
      backgroundColor: colors[idx % colors.length],
      fill: false,
      tension: 0.2,
      spanGaps: true,
    }
  })

  const data = {
    labels: labels.map((d) => d.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })),
    datasets,
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
          label: (ctx: any) => `${ctx.dataset.label}: ${Number(ctx.raw ?? 0).toFixed(2)} ${valueLabel ?? ''}`.trim(),
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
          callback: (val: any) => Number(val).toFixed(1),
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
