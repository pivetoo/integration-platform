import { createRoot } from 'react-dom/client'
import './tailwind.css'
import 'archon-ui/styles'
import './theme.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <App />
)
