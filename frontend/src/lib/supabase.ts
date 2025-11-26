import { createClient } from '@supabase/supabase-js'

const supabaseUrl = import.meta.env.VITE_SUPABASE_URL || 'https://ikbpmyqftmlgyvfysucs.supabase.co'
const supabaseAnonKey = import.meta.env.VITE_SUPABASE_ANON_KEY || 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImlrYnBteXFmdG1sZ3l2ZnlzdWNzIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjM4NzQyNjEsImV4cCI6MjA3OTQ1MDI2MX0.jvZwCyyd5UhzLyLnVAWB2tr7JG5cDAtTKnGch-14xFM'

export const supabase = createClient(supabaseUrl, supabaseAnonKey)

