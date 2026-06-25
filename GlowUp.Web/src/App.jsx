import { useState } from 'react'
import {
  cerrarSesion,
  guardarSesion,
  iniciarSesion,
  obtenerSesion,
  registrarUsuario,
} from './services/authService'
import glowUpLogo from './assets/glowup-rd-logo.png'
import Dashboard from './components/Dashboard'
import './App.css'

const initialLogin = { correo: '', password: '' }
const initialRegister = { nombre: '', apellido: '', correo: '', password: '' }

function EyeIcon({ hidden }) {
  return hidden ? (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="M3 3l18 18M10.6 10.7a2 2 0 002.8 2.8M9.9 4.2A10.7 10.7 0 0112 4c5 0 8.5 4.5 9.5 6a1.8 1.8 0 010 2 16.8 16.8 0 01-2.4 3M6.2 6.3A16.5 16.5 0 002.5 10a1.8 1.8 0 000 2c1 1.5 4.5 6 9.5 6a9.8 9.8 0 004.1-.9" />
    </svg>
  ) : (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="M2.5 10a1.8 1.8 0 000 2c1 1.5 4.5 6 9.5 6s8.5-4.5 9.5-6a1.8 1.8 0 000-2C20.5 8.5 17 4 12 4s-8.5 4.5-9.5 6z" />
      <circle cx="12" cy="11" r="3" />
    </svg>
  )
}

function ArrowIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M5 12h14M13 6l6 6-6 6" /></svg>
}

function GoogleIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path fill="#4285f4" d="M21.6 12.2c0-.7-.1-1.4-.2-2H12v3.9h5.4a4.6 4.6 0 01-2 3v2.5h3.3c1.9-1.8 2.9-4.4 2.9-7.4z" />
      <path fill="#34a853" d="M12 22c2.7 0 5-.9 6.7-2.4l-3.3-2.5c-.9.6-2.1 1-3.4 1-2.6 0-4.8-1.8-5.6-4.1H3v2.6A10 10 0 0012 22z" />
      <path fill="#fbbc05" d="M6.4 14a6 6 0 010-3.9V7.4H3a10 10 0 000 9.2L6.4 14z" />
      <path fill="#ea4335" d="M12 5.9c1.5 0 2.8.5 3.9 1.5l2.9-2.8A9.7 9.7 0 0012 2a10 10 0 00-9 5.4L6.4 10c.8-2.4 3-4.1 5.6-4.1z" />
    </svg>
  )
}

function SparkIcon() {
  return <svg viewBox="0 0 32 32" aria-hidden="true"><path d="M16 2c.7 8.1 5.9 13.3 14 14-8.1.7-13.3 5.9-14 14-.7-8.1-5.9-13.3-14-14C10.1 15.3 15.3 10.1 16 2z" /></svg>
}

function App() {
  const [mode, setMode] = useState('login')
  const [loginData, setLoginData] = useState(initialLogin)
  const [registerData, setRegisterData] = useState(initialRegister)
  const [showPassword, setShowPassword] = useState(false)
  const [rememberMe, setRememberMe] = useState(false)
  const [loading, setLoading] = useState(false)
  const [feedback, setFeedback] = useState(null)
  const [session, setSession] = useState(() => obtenerSesion())

  const isLogin = mode === 'login'
  const formData = isLogin ? loginData : registerData

  function changeMode(nextMode) {
    setMode(nextMode)
    setFeedback(null)
    setShowPassword(false)
  }

  function updateField(event) {
    const { name, value } = event.target
    const updater = isLogin ? setLoginData : setRegisterData
    updater((current) => ({ ...current, [name]: value }))
    setFeedback(null)
  }

  async function handleSubmit(event) {
    event.preventDefault()
    setLoading(true)
    setFeedback(null)

    try {
      if (isLogin) {
        const response = await iniciarSesion({
          correo: loginData.correo.trim(),
          password: loginData.password,
        })
        guardarSesion(response, rememberMe)
        setSession(response)
      } else {
        await registrarUsuario({
          nombre: registerData.nombre.trim(),
          apellido: registerData.apellido.trim(),
          correo: registerData.correo.trim(),
          password: registerData.password,
        })
        setLoginData({ correo: registerData.correo, password: '' })
        setRegisterData(initialRegister)
        setMode('login')
        setFeedback({ type: 'success', message: 'Tu cuenta fue creada. Ya puedes iniciar sesión.' })
      }
    } catch (error) {
      setFeedback({ type: 'error', message: error.message })
    } finally {
      setLoading(false)
    }
  }

  function closeSession() {
    cerrarSesion()
    setSession(null)
    setLoginData(initialLogin)
    setFeedback(null)
  }

  if (session) {
    return <Dashboard session={session} onLogout={closeSession} />
  }

  return (
    <main className="auth-page">
      <div className="ambient ambient-one" />
      <div className="ambient ambient-two" />

      <section className="auth-shell" aria-label="Acceso a GlowUp">
        <aside className="brand-panel">
          <header className="brand-header">
            <a className="brand-logo-link" href="/" aria-label="GlowUp RD inicio">
              <img className="brand-logo" src={glowUpLogo} alt="GlowUp RD" />
            </a>
          </header>

          <div className="brand-copy">
            <h1>Tu negocio de belleza,<br /><span>elevado al siguiente nivel.</span></h1>
          </div>
        </aside>

        <section className="form-panel">
          {session ? (
            <div className="welcome-state">
              <div className="welcome-icon"><SparkIcon /></div>
              <span className="eyebrow">SESIÓN INICIADA</span>
              <h2>Hola, {session.usuario.nombre}.</h2>
              <p>Tu espacio está listo. Acabas de dar el primer paso de esta experiencia.</p>
              <button className="primary-button" type="button" onClick={closeSession}>
                Cerrar sesión <ArrowIcon />
              </button>
            </div>
          ) : (
            <div className="form-content">
              <div className="mobile-brand">
                <img src={glowUpLogo} alt="GlowUp RD" />
              </div>

              <div className="form-heading">
                <span className="eyebrow">{isLogin ? 'BIENVENIDO DE NUEVO' : 'EMPIEZA TU HISTORIA'}</span>
                <h2>{isLogin ? 'Qué bueno verte.' : 'Crea tu cuenta.'}</h2>
                {isLogin && <p>Ingresa tus datos para continuar tu experiencia.</p>}
              </div>

              <div className="mode-switch" role="tablist" aria-label="Tipo de acceso">
                <button type="button" role="tab" aria-selected={isLogin} className={isLogin ? 'active' : ''} onClick={() => changeMode('login')}>Iniciar sesión</button>
                <button type="button" role="tab" aria-selected={!isLogin} className={!isLogin ? 'active' : ''} onClick={() => changeMode('register')}>Crear cuenta</button>
              </div>

              <form onSubmit={handleSubmit}>
                {!isLogin && (
                  <div className="field-row">
                    <label className="field">
                      <span>Nombre</span>
                      <input name="nombre" value={formData.nombre} onChange={updateField} placeholder="Tu nombre" autoComplete="given-name" maxLength="100" required />
                    </label>
                    <label className="field">
                      <span>Apellido</span>
                      <input name="apellido" value={formData.apellido} onChange={updateField} placeholder="Tu apellido" autoComplete="family-name" maxLength="100" required />
                    </label>
                  </div>
                )}

                <label className="field">
                  <span>Correo electrónico</span>
                  <input type="email" name="correo" value={formData.correo} onChange={updateField} placeholder="nombre@correo.com" autoComplete="email" maxLength="255" required />
                </label>

                <label className="field">
                  <span>Contraseña</span>
                  <span className="password-input">
                    <input type={showPassword ? 'text' : 'password'} name="password" value={formData.password} onChange={updateField} placeholder={isLogin ? 'Ingresa tu contraseña' : 'Mínimo 8 caracteres'} autoComplete={isLogin ? 'current-password' : 'new-password'} minLength={isLogin ? undefined : 8} maxLength="100" required />
                    <button type="button" className="eye-button" onClick={() => setShowPassword((visible) => !visible)} aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'}>
                      <EyeIcon hidden={showPassword} />
                    </button>
                  </span>
                </label>

                {isLogin && (
                  <div className="form-options">
                    <label className="checkbox"><input type="checkbox" checked={rememberMe} onChange={(event) => setRememberMe(event.target.checked)} /><span />Recordarme</label>
                    <button className="text-button" type="button">¿Olvidaste tu contraseña?</button>
                  </div>
                )}

                {feedback && <div className={`feedback ${feedback.type}`} role="alert"><span>{feedback.type === 'success' ? '✓' : '!'}</span>{feedback.message}</div>}

                <button className="primary-button" type="submit" disabled={loading}>
                  <span>{loading ? 'Procesando...' : isLogin ? 'Entrar a GlowUp' : 'Crear mi cuenta'}</span>
                  {!loading && <ArrowIcon />}
                </button>
              </form>

              <div className="auth-divider"><span>o continúa con</span></div>
              <button className="google-button" type="button">
                <GoogleIcon />
                {isLogin ? 'Iniciar sesión con Google' : 'Registrarme con Google'}
              </button>

            </div>
          )}
        </section>
      </section>
    </main>
  )
}

export default App
