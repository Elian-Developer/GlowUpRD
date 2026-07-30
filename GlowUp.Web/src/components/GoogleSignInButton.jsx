import { useEffect, useRef, useState } from 'react'

const CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID ?? ''
const SCRIPT_SRC = 'https://accounts.google.com/gsi/client'

let scriptPromise = null

function loadGoogleScript() {
  if (scriptPromise) return scriptPromise

  scriptPromise = new Promise((resolve, reject) => {
    const existing = document.querySelector(`script[src="${SCRIPT_SRC}"]`)
    if (existing) {
      existing.addEventListener('load', () => resolve(window.google))
      return
    }

    const script = document.createElement('script')
    script.src = SCRIPT_SRC
    script.async = true
    script.defer = true
    script.onload = () => resolve(window.google)
    script.onerror = () => reject(new Error('No se pudo cargar Google Identity Services.'))
    document.head.appendChild(script)
  })

  return scriptPromise
}

export default function GoogleSignInButton({ onCredential, onError }) {
  const containerRef = useRef(null)
  const [ready, setReady] = useState(false)

  useEffect(() => {
    if (!CLIENT_ID) return

    let cancelled = false

    loadGoogleScript()
      .then((google) => {
        if (cancelled || !containerRef.current) return
        google.accounts.id.initialize({
          client_id: CLIENT_ID,
          callback: (response) => onCredential(response.credential),
        })
        google.accounts.id.renderButton(containerRef.current, {
          theme: 'outline',
          size: 'large',
          width: 320,
          text: 'continue_with',
        })
        setReady(true)
      })
      .catch((error) => onError?.(error.message))

    return () => { cancelled = true }
  }, [onCredential, onError])

  if (!CLIENT_ID) {
    return (
      <button className="google-button" type="button" disabled title="Falta configurar VITE_GOOGLE_CLIENT_ID">
        Iniciar sesión con Google (no configurado)
      </button>
    )
  }

  return <div ref={containerRef} className="google-button-slot" aria-busy={!ready} />
}
