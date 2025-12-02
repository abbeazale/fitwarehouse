import { useEffect, useMemo, useState } from 'react'
import Header from './components/Header'
import { Line, Bar } from 'react-chartjs-2'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  Tooltip,
  Legend,
} from 'chart.js'

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, BarElement, Tooltip, Legend)

type WeeklySeriesPoint = {
  weekStart: string
  ageGroup: string
  totalDistanceKm: number
  runnerCount: number
  avgDistanceKmPerRunner: number
}

type OverviewResponse = {
  weeks: number
  series: WeeklySeriesPoint[]
  kpis: {
    totalDistanceKm: number
    avgWeeklyDistanceKm: number
    totalRunners: number
    latestWeekDistanceKm: number
  }
}

type TopCountryEntry = {
  rank: number
  country: string
  totalDistanceKm: number
  avgPaceMinPerKm: number | null
}

type TopCountriesResponse = {
  weeks: number
  countries: TopCountryEntry[]
}

type MajorGenderEntry = {
  majorName: string
  gender: string
  runnerCount: number
}

type MajorGenderResponse = {
  series: MajorGenderEntry[]
}

const normalizeBaseUrl = (value: unknown, fallback: string) => {
  const url = typeof value === 'string' && value.trim().length > 0 ? value : fallback
  return url.replace(/\/$/, '')
}

const rawApiBase =
  import.meta.env.VITE_BACKEND_BASE_URL ?? import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5018'
const apiBaseUrl = normalizeBaseUrl(rawApiBase, 'http://localhost:5018')

export default function Dashboard() {
  const [data, setData] = useState<OverviewResponse | null>(null)
  const [topCountries, setTopCountries] = useState<TopCountriesResponse | null>(null)
  const [majorGender, setMajorGender] = useState<MajorGenderResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    setLoading(true)
    setError(null)

    const overviewPromise = fetch(`${apiBaseUrl}/api/analytics/overview?weeks=12`, { signal: controller.signal })
      .then(async (res) => {
        if (!res.ok) {
          const txt = await res.text()
          throw new Error(txt || `HTTP ${res.status}`)
        }
        return res.json() as Promise<OverviewResponse>
      })
      .then(setData)

    const countriesPromise = fetch(`${apiBaseUrl}/api/analytics/top-countries?weeks=12&limit=50`, {
      signal: controller.signal,
    })
      .then(async (res) => {
        if (!res.ok) {
          const txt = await res.text()
          throw new Error(txt || `HTTP ${res.status}`)
        }
        return res.json() as Promise<TopCountriesResponse>
      })
      .then(setTopCountries)

    Promise.all([overviewPromise, countriesPromise])
      .then(() =>
        fetch(`${apiBaseUrl}/api/analytics/major-gender-distribution?limit=10`, { signal: controller.signal })
          .then(async (res) => {
            if (!res.ok) {
              const txt = await res.text()
              throw new Error(txt || `HTTP ${res.status}`)
            }
            return res.json() as Promise<MajorGenderResponse>
          })
          .then(setMajorGender)
      )
      .catch((err) => {
        if (err.name !== 'AbortError') setError('Failed to load analytics')
      })
      .finally(() => setLoading(false))

    return () => controller.abort()
  }, [])

  const groupedSeries = useMemo(() => {
    if (!data) return {}
    const map: Record<string, { week: Date; value: number }[]> = {}
    data.series.forEach((pt) => {
      const key = pt.ageGroup || 'Unknown'
      if (!map[key]) map[key] = []
      map[key].push({ week: new Date(pt.weekStart), value: pt.totalDistanceKm })
    })
    Object.values(map).forEach((arr) => arr.sort((a, b) => a.week.getTime() - b.week.getTime()))
    return map
  }, [data])

  const chartWeeks = useMemo(() => {
    if (!data) return []
    const unique = Array.from(new Set(data.series.map((s) => s.weekStart)))
    return unique.sort()
  }, [data])

  const colors = ['#10b981', '#22d3ee', '#a855f7', '#f97316', '#38bdf8', '#f43f5e']
  const ageGroups = Object.keys(groupedSeries)

  return (
    <div className="min-h-screen bg-slate-950 text-slate-50">
      <Header />

      <main className="max-w-6xl mx-auto px-4 sm:px-8 py-8 space-y-8">
        <div>
          <h1 className="text-3xl font-semibold tracking-tight mb-2">Overview</h1>
          <p className="text-slate-400">Distance trends by age group and quick KPIs</p>
        </div>

        {loading ? (
          <div className="flex items-center justify-center py-16 text-slate-400">Loading...</div>
        ) : error ? (
          <div className="bg-red-500/10 border border-red-500/30 text-red-200 px-4 py-3 rounded-lg">{error}</div>
        ) : data ? (
          <>
            <section className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
              <KpiCard label="Total Distance" value={formatNumber(data.kpis.totalDistanceKm)} suffix="km" />
              <KpiCard label="Avg Weekly Distance / Runner" value={formatNumber(data.kpis.avgWeeklyDistanceKm)} suffix="km" />
              <KpiCard label="Total Runners" value={formatNumber(data.kpis.totalRunners)} />
              <KpiCard label="Latest Week Distance" value={formatNumber(data.kpis.latestWeekDistanceKm)} suffix="km" />
            </section>

            <section className="bg-slate-900 border border-slate-800 rounded-2xl p-6">
              <div className="flex items-center justify-between mb-4">
                <div>
                  <h2 className="text-xl font-semibold">Weekly distance by age group</h2>
                  <p className="text-slate-500 text-sm">Stacked multi-line across the last {data.weeks} weeks</p>
                </div>
              </div>
              {ageGroups.length === 0 ? (
                <div className="text-slate-500">No data</div>
              ) : (
                <LineChart height={360} series={groupedSeries} colors={colors} weeks={chartWeeks} />
              )}
            </section>

            <section className="bg-slate-900 border border-slate-800 rounded-2xl p-6">
              <div className="flex items-center justify-between mb-4">
                <div>
                  <h2 className="text-xl font-semibold">Top countries by distance</h2>
                  <p className="text-slate-500 text-sm">Last {topCountries?.weeks ?? data.weeks} weeks</p>
                </div>
              </div>
              {!topCountries || topCountries.countries.length === 0 ? (
                <div className="text-slate-500">No data</div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="min-w-full text-sm text-left text-slate-300">
                    <thead className="text-xs uppercase text-slate-500 border-b border-slate-800">
                      <tr>
                        <th className="px-3 py-2">Rank</th>
                        <th className="px-3 py-2">Country</th>
                        <th className="px-3 py-2">Total Distance (km)</th>
                        <th className="px-3 py-2">Avg Pace (min/km)</th>
                      </tr>
                    </thead>
                    <tbody>
                      {topCountries.countries.map((c) => (
                        <tr key={c.rank} className="border-b border-slate-800/60">
                          <td className="px-3 py-2 text-slate-400">{c.rank}</td>
                          <td className="px-3 py-2 font-medium text-slate-100">{c.country}</td>
                          <td className="px-3 py-2">{c.totalDistanceKm.toFixed(0)}</td>
                          <td className="px-3 py-2">{c.avgPaceMinPerKm ? c.avgPaceMinPerKm.toFixed(2) : '—'}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </section>

            <section className="bg-slate-900 border border-slate-800 rounded-2xl p-6">
              <div className="flex items-center justify-between mb-4">
                <div>
                  <h2 className="text-xl font-semibold">Gender distribution by major</h2>
                  <p className="text-slate-500 text-sm">Top 10 majors by runner count.</p>
                </div>
              </div>
              {!majorGender || majorGender.series.length === 0 ? (
                <div className="text-slate-500">No data</div>
              ) : (
                <MajorGenderBar data={majorGender.series} />
              )}
            </section>
          </>
        ) : null}
      </main>
    </div>
  )
}

type KpiCardProps = {
  label: string
  value: string
  suffix?: string
}

function KpiCard({ label, value, suffix }: KpiCardProps) {
  return (
    <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5">
      <p className="text-sm text-slate-500">{label}</p>
      <div className="mt-2 text-2xl font-semibold text-teal-300">
        {value}
        {suffix ? <span className="text-slate-400 text-base ml-1">{suffix}</span> : null}
      </div>
    </div>
  )
}

type LineChartProps = {
  height: number
  series: Record<string, { week: Date; value: number }[]>
  colors: string[]
  weeks: string[]
}

function LineChart({ height, series, colors, weeks }: LineChartProps) {
  const labels = weeks.map((w) => new Date(w)).sort((a, b) => a.getTime() - b.getTime())

  const datasets = Object.entries(series).map(([ageGroup, points], idx) => {
    const map = new Map(points.map((p) => [p.week.toDateString(), p.value]))
    return {
      label: ageGroup,
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
          label: (ctx: any) => `${ctx.dataset.label}: ${Number(ctx.raw ?? 0).toFixed(1)} km`,
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
          callback: (val: any) => formatNumber(Number(val)),
        },
        grid: { color: '#1e293b' },
        beginAtZero: true,
      },
    },
  }

  return (
    <div className="relative" style={{ height }}>
      <Line data={data} options={options} />
    </div>
  )
}

const formatNumber = (value: number) => {
  if (value >= 1_000_000) return `${(value / 1_000_000).toFixed(1)}m`
  if (value >= 1_000) return `${(value / 1_000).toFixed(1)}k`
  return value.toFixed(0)
}

type MajorGenderBarProps = {
  data: MajorGenderEntry[]
}

function MajorGenderBar({ data }: MajorGenderBarProps) {
  const majors = Array.from(new Set(data.map((d) => d.majorName)))
  const genders = Array.from(new Set(data.map((d) => d.gender))).sort()
  const colors = ['#10b981', '#22d3ee', '#f97316', '#a855f7', '#38bdf8', '#f43f5e']

  const datasets = genders.map((g, idx) => {
    const label = g === 'M' ? 'Male' : g === 'F' ? 'Female' : 'Unknown'
    return {
      label,
      data: majors.map((m) => {
        const row = data.find((d) => d.majorName === m && d.gender === g)
        return row ? row.runnerCount : 0
      }),
      backgroundColor: colors[idx % colors.length],
      stack: 'gender',
    }
  })

  const chartData = {
    labels: majors,
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
          label: (ctx: any) => `${ctx.dataset.label}: ${ctx.raw}`,
        },
      },
    },
    scales: {
      x: {
        ticks: { color: '#94a3b8' },
        grid: { color: '#1e293b' },
        stacked: true,
      },
      y: {
        ticks: {
          color: '#94a3b8',
          callback: (val: any) => Number(val).toFixed(0),
        },
        grid: { color: '#1e293b' },
        beginAtZero: true,
        stacked: true,
      },
    },
  }

  return (
    <div className="relative" style={{ height: 400 }}>
      <Bar data={chartData} options={options} />
    </div>
  )
}
