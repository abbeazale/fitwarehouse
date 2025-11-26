import { useState, type FormEvent, useEffect } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { supabase } from './lib/supabase'

export default function Signup() {
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const navigate = useNavigate()

  useEffect(() => {
    supabase.auth.getSession().then(({ data: { session } }) => {
      if (session?.user) {
        navigate('/dashboard')
      }
    })
  }, [navigate])

  const handleSignUp = async (e: FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setError(null)

    if (password !== confirmPassword) {
      setError('Passwords do not match')
      setLoading(false)
      return
    }

    if (password.length < 6) {
      setError('Password must be at least 6 characters')
      setLoading(false)
      return
    }

    const { error } = await supabase.auth.signUp({
      email,
      password,
      options: {
        data: {
          full_name: name,
        },
      },
    })

    if (error) {
      setError(error.message)
      setLoading(false)
    } else {
      // Redirect to dashboard with active session
      navigate('/dashboard')
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center p-8 bg-slate-950 relative overflow-hidden">
      <div className="fixed inset-0 bg-linear-to-br from-teal-500/10 via-transparent to-transparent" />
      <div className="absolute top-0 right-0 w-96 h-96 bg-teal-500/15 rounded-full blur-3xl opacity-50" />
      <div className="absolute bottom-0 left-0 w-80 h-80 bg-purple-500/10 rounded-full blur-3xl opacity-50" />

      <form onSubmit={handleSignUp} className="relative z-10 bg-slate-900 border border-slate-800 rounded-3xl p-10 w-full max-w-md shadow-2xl">
        {/* Header */}
        <svg className="w-14 h-14 mx-auto mb-4 bg-teal-500/15 rounded-xl flex items-center justify-center text-teal-500 p-3.5" viewBox="0 0 24 24" fill="currentColor">
          <path d="M13.5 5.5c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zM9.8 8.9L7 23h2.1l1.8-8 2.1 2v6h2v-7.5l-2.1-2 .6-3C14.8 12 16.8 13 19 13v-2c-1.9 0-3.5-1-4.3-2.4l-1-1.6c-.4-.6-1-1-1.7-1-.3 0-.5.1-.8.1L6 8.3V13h2V9.6l1.8-.7" />
        </svg>
        <h1 className="text-3xl font-semibold mb-2 text-slate-50 tracking-tight text-center">Create Account</h1>
        <p className="text-slate-400 text-center mb-8">Start tracking your health data today</p>

        {/* Name Field */}
        <label htmlFor="name" className="block text-sm font-medium text-slate-400 mb-2">Full Name</label>
        <input
          type="text"
          id="name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="John Doe"
          required
          autoComplete="name"
          className="w-full bg-slate-800 border border-slate-700 rounded-xl px-4 py-3 text-slate-50 placeholder-slate-500 focus:outline-none focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 transition mb-5"
        />

        {/* Email Field */}
        <label htmlFor="email" className="block text-sm font-medium text-slate-400 mb-2">Email</label>
        <input
          type="email"
          id="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="you@example.com"
          required
          autoComplete="email"
          className="w-full bg-slate-800 border border-slate-700 rounded-xl px-4 py-3 text-slate-50 placeholder-slate-500 focus:outline-none focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 transition mb-5"
        />

        {/* Password Field */}
        <label htmlFor="password" className="block text-sm font-medium text-slate-400 mb-2">Password</label>
        <input
          type="password"
          id="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          placeholder="At least 6 characters"
          required
          autoComplete="new-password"
          className="w-full bg-slate-800 border border-slate-700 rounded-xl px-4 py-3 text-slate-50 placeholder-slate-500 focus:outline-none focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 transition mb-5"
        />

        {/* Confirm Password Field */}
        <label htmlFor="confirmPassword" className="block text-sm font-medium text-slate-400 mb-2">Confirm Password</label>
        <input
          type="password"
          id="confirmPassword"
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
          placeholder="Confirm your password"
          required
          autoComplete="new-password"
          className="w-full bg-slate-800 border border-slate-700 rounded-xl px-4 py-3 text-slate-50 placeholder-slate-500 focus:outline-none focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 transition mb-5"
        />

        {/* Error Message */}
        {error && (
          <p className="bg-red-500/15 text-red-500 px-4 py-3 rounded-lg text-sm flex items-center gap-2 mb-5">
            <svg className="w-5 h-5 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <circle cx="12" cy="12" r="10" />
              <line x1="15" y1="9" x2="9" y2="15" />
              <line x1="9" y1="9" x2="15" y2="15" />
            </svg>
            {error}
          </p>
        )}

        {/* Submit Button */}
        <button
          type="submit"
          disabled={loading}
          className="w-full bg-teal-500 hover:bg-teal-600 text-white font-medium py-3 rounded-xl transition-all hover:shadow-lg hover:shadow-teal-500/30 hover:-translate-y-0.5 disabled:opacity-60 disabled:cursor-not-allowed disabled:hover:translate-y-0 flex items-center justify-center gap-2"
        >
          {loading && (
            <svg className="w-5 h-5 animate-spin" viewBox="0 0 24 24" fill="none">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
            </svg>
          )}
          {loading ? 'Creating account...' : 'Create Account'}
        </button>

        {/* Footer */}
        <p className="mt-6 pt-6 border-t border-slate-800 text-center text-slate-400 text-sm">
          Already have an account?{' '}
          <Link to="/login" className="text-teal-500 hover:text-teal-400 font-medium transition">
            Sign in
          </Link>
        </p>
      </form>
    </div>
  )
}
