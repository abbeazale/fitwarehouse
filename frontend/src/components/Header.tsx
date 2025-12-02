import { Link, useLocation } from 'react-router-dom'

export default function Header() {
  const location = useLocation()
  const isActive = (path: string) => location.pathname === path

  const navLink = (to: string, label: string) => (
    <Link
      to={to}
      className={`px-3 py-2 rounded-lg text-sm font-medium transition ${
        isActive(to)
          ? 'bg-teal-500/15 text-teal-200 border border-teal-500/40'
          : 'text-slate-300 hover:text-teal-200 border border-transparent hover:border-slate-700'
      }`}
    >
      {label}
    </Link>
  )

  return (
    <nav className="sticky top-0 z-50 flex items-center justify-between px-6 sm:px-8 py-4 bg-slate-900 border-b border-slate-800">
      <div className="flex items-center gap-3 text-slate-50 font-semibold text-lg">
        <svg className="w-7 h-7 text-teal-500" viewBox="0 0 24 24" fill="currentColor">
          <path d="M13.5 5.5c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zM9.8 8.9L7 23h2.1l1.8-8 2.1 2v6h2v-7.5l-2.1-2 .6-3C14.8 12 16.8 13 19 13v-2c-1.9 0-3.5-1-4.3-2.4l-1-1.6c-.4-.6-1-1-1.7-1-.3 0-.5.1-.8.1L6 8.3V13h2V9.6l1.8-.7" />
        </svg>
        <span>FitWarehouse</span>
      </div>

      <div className="flex items-center gap-2 sm:gap-3">
        {navLink('/', 'Overview')}
        {navLink('/trends', 'Trends')}
        {navLink('/athlete', 'Athlete')}
      </div>
    </nav>
  )
}
