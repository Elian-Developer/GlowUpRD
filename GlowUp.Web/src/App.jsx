import { useState } from 'react'
import {
  cerrarSesion,
  guardarSesion,
  iniciarSesion,
  iniciarSesionConGoogle,
  obtenerSesion,
  registrarNegocioYPropietario,
  restablecerPassword,
  solicitarRestablecimiento,
} from './services/authService'
import glowUpLogo from './assets/glowup-rd-logo.png'
import Dashboard from './components/Dashboard'
import GoogleSignInButton from './components/GoogleSignInButton'
import './App.css'

const initialLogin = { correo: '', password: '' }
const initialRegister = {
  nombreNegocio: '',
  tipoNegocio: 'salon',
  direccion: '',
  ciudad: '',
  provincia: '',
  nombre: '',
  apellido: '',
  correo: '',
  password: '',
}

const tiposNegocio = [
  ['salon', 'Salón'],
  ['barbershop', 'Barbería'],
  ['spa', 'Spa'],
  ['mixed', 'Mixto'],
]

function buildRegistrarNegocioPayload(data) {
  return {
    nombre: data.nombreNegocio.trim(),
    tipoNegocio: data.tipoNegocio,
    direccion: data.direccion.trim(),
    ciudad: data.ciudad.trim(),
    provincia: data.provincia.trim(),
    nombrePropietario: data.nombre.trim(),
    apellidoPropietario: data.apellido.trim(),
    correoPropietario: data.correo.trim(),
    password: data.password,
  }
}

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

function SparkIcon() {
  return <svg viewBox="0 0 32 32" aria-hidden="true"><path d="M16 2c.7 8.1 5.9 13.3 14 14-8.1.7-13.3 5.9-14 14-.7-8.1-5.9-13.3-14-14C10.1 15.3 15.3 10.1 16 2z" /></svg>
}

function ForgotPasswordForm({ onBack }) {
  const [correo, setCorreo] = useState('')
  const [loading, setLoading] = useState(false)
  const [feedback, setFeedback] = useState(null)

  async function submit(event) {
    event.preventDefault()
    setLoading(true)
    setFeedback(null)
    try {
      await solicitarRestablecimiento(correo.trim())
      setFeedback({ type: 'success', message: 'Si el correo existe, te enviamos un enlace para restablecer tu contraseña.' })
    } catch (error) {
      setFeedback({ type: 'error', message: error.message })
    } finally {
      setLoading(false)
    }
  }

  return (
    <form onSubmit={submit}>
      <label className="field">
        <span>Correo electrónico</span>
        <input type="email" value={correo} onChange={(event) => setCorreo(event.target.value)} placeholder="nombre@correo.com" maxLength="255" required />
      </label>

      {feedback && <div className={`feedback ${feedback.type}`} role="alert"><span>{feedback.type === 'success' ? '✓' : '!'}</span>{feedback.message}</div>}

      <button className="primary-button" type="submit" disabled={loading}>
        <span>{loading ? 'Enviando...' : 'Enviar enlace'}</span>
        {!loading && <ArrowIcon />}
      </button>
      <button className="text-button" type="button" onClick={onBack}>Volver a iniciar sesión</button>
    </form>
  )
}

function ResetPasswordScreen({ token }) {
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [feedback, setFeedback] = useState(null)
  const [done, setDone] = useState(false)

  async function submit(event) {
    event.preventDefault()
    if (password !== confirmPassword) {
      setFeedback({ type: 'error', message: 'Las contraseñas no coinciden.' })
      return
    }
    setLoading(true)
    setFeedback(null)
    try {
      await restablecerPassword(token, password)
      setDone(true)
    } catch (error) {
      setFeedback({ type: 'error', message: error.message })
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className="auth-page">
      <div className="ambient ambient-one" />
      <div className="ambient ambient-two" />
      <section className="auth-shell" aria-label="Restablecer contraseña">
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
          <div className="form-content">
            <div className="mobile-brand"><img src={glowUpLogo} alt="GlowUp RD" /></div>
            {done ? (
              <div className="welcome-state">
                <div className="welcome-icon"><SparkIcon /></div>
                <span className="eyebrow">LISTO</span>
                <h2>Tu contraseña fue actualizada.</h2>
                <p>Ya puedes iniciar sesión con tu nueva contraseña.</p>
                <a className="primary-button" href="/">Ir a iniciar sesión</a>
              </div>
            ) : (
              <>
                <div className="form-heading">
                  <span className="eyebrow">RESTABLECER CONTRASEÑA</span>
                  <h2>Crea una nueva contraseña.</h2>
                </div>
                <form onSubmit={submit}>
                  <label className="field">
                    <span>Nueva contraseña</span>
                    <input type="password" value={password} onChange={(event) => setPassword(event.target.value)} minLength="8" maxLength="100" placeholder="Mínimo 8 caracteres" required />
                  </label>
                  <label className="field">
                    <span>Confirmar contraseña</span>
                    <input type="password" value={confirmPassword} onChange={(event) => setConfirmPassword(event.target.value)} minLength="8" maxLength="100" placeholder="Repite tu contraseña" required />
                  </label>
                  {feedback && <div className={`feedback ${feedback.type}`} role="alert"><span>!</span>{feedback.message}</div>}
                  <button className="primary-button" type="submit" disabled={loading}>
                    <span>{loading ? 'Guardando...' : 'Guardar nueva contraseña'}</span>
                    {!loading && <ArrowIcon />}
                  </button>
                </form>
              </>
            )}
          </div>
        </section>
      </section>
    </main>
  )
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
  const [resetToken] = useState(() => new URLSearchParams(window.location.search).get('token'))

  const isLogin = mode === 'login'
  const isForgot = mode === 'forgot'
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
      const response = isLogin
        ? await iniciarSesion({ correo: loginData.correo.trim(), password: loginData.password })
        : await registrarNegocioYPropietario(buildRegistrarNegocioPayload(registerData))
      guardarSesion(response, rememberMe)
      setSession(response)
    } catch (error) {
      setFeedback({ type: 'error', message: error.message })
    } finally {
      setLoading(false)
    }
  }

  async function handleGoogleCredential(credential) {
    setLoading(true)
    setFeedback(null)
    try {
      const response = await iniciarSesionConGoogle(credential)
      guardarSesion(response, rememberMe)
      setSession(response)
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

  if (resetToken) {
    return <ResetPasswordScreen token={resetToken} />
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

              {isForgot ? (
                <>
                  <div className="form-heading">
                    <span className="eyebrow">RECUPERAR ACCESO</span>
                    <h2>¿Olvidaste tu contraseña?</h2>
                    <p>Ingresa tu correo y te enviaremos un enlace para restablecerla.</p>
                  </div>
                  <ForgotPasswordForm onBack={() => changeMode('login')} />
                </>
              ) : (
              <>
              <div className="form-heading">
                <span className="eyebrow">{isLogin ? 'BIENVENIDO DE NUEVO' : 'EMPIEZA TU HISTORIA'}</span>
                <h2>{isLogin ? 'Qué bueno verte.' : 'Registra tu negocio.'}</h2>
                <p>{isLogin ? 'Ingresa tus datos para continuar tu experiencia.' : 'Crea el perfil de tu negocio y tu cuenta de propietario en un solo paso.'}</p>
              </div>

              <div className="mode-switch" role="tablist" aria-label="Tipo de acceso">
                <button type="button" role="tab" aria-selected={isLogin} className={isLogin ? 'active' : ''} onClick={() => changeMode('login')}>Iniciar sesión</button>
                <button type="button" role="tab" aria-selected={!isLogin} className={!isLogin ? 'active' : ''} onClick={() => changeMode('register')}>Registrar negocio</button>
              </div>

              <form onSubmit={handleSubmit}>
                {!isLogin && (
                  <>
                    <label className="field">
                      <span>Nombre del negocio</span>
                      <input name="nombreNegocio" value={registerData.nombreNegocio} onChange={updateField} placeholder="Ej. Barbería Baronil" maxLength="150" required />
                    </label>

                    <label className="field">
                      <span>Tipo de negocio</span>
                      <select name="tipoNegocio" value={registerData.tipoNegocio} onChange={updateField} required>
                        {tiposNegocio.map(([value, label]) => <option key={value} value={value}>{label}</option>)}
                      </select>
                    </label>

                    <label className="field">
                      <span>Dirección de tu sucursal principal</span>
                      <input name="direccion" value={registerData.direccion} onChange={updateField} placeholder="Calle, número, sector" maxLength="255" required />
                    </label>

                    <div className="field-row">
                      <label className="field">
                        <span>Ciudad</span>
                        <input name="ciudad" value={registerData.ciudad} onChange={updateField} placeholder="Ej. Santo Domingo" maxLength="100" required />
                      </label>
                      <label className="field">
                        <span>Provincia</span>
                        <input name="provincia" value={registerData.provincia} onChange={updateField} placeholder="Ej. Distrito Nacional" maxLength="100" required />
                      </label>
                    </div>

                    <div className="field-row">
                      <label className="field">
                        <span>Tu nombre</span>
                        <input name="nombre" value={formData.nombre} onChange={updateField} placeholder="Tu nombre" autoComplete="given-name" maxLength="100" required />
                      </label>
                      <label className="field">
                        <span>Tu apellido</span>
                        <input name="apellido" value={formData.apellido} onChange={updateField} placeholder="Tu apellido" autoComplete="family-name" maxLength="100" required />
                      </label>
                    </div>
                  </>
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
                    <button className="text-button" type="button" onClick={() => changeMode('forgot')}>¿Olvidaste tu contraseña?</button>
                  </div>
                )}

                {feedback && <div className={`feedback ${feedback.type}`} role="alert"><span>{feedback.type === 'success' ? '✓' : '!'}</span>{feedback.message}</div>}

                <button className="primary-button" type="submit" disabled={loading}>
                  <span>{loading ? 'Procesando...' : isLogin ? 'Entrar a GlowUp' : 'Registrar mi negocio'}</span>
                  {!loading && <ArrowIcon />}
                </button>
              </form>

              {isLogin && (
                <>
                  <div className="auth-divider"><span>o continúa con</span></div>
                  <GoogleSignInButton
                    onCredential={handleGoogleCredential}
                    onError={(message) => setFeedback({ type: 'error', message })}
                  />
                </>
              )}
              </>
              )}

            </div>
          )}
        </section>
      </section>
    </main>
  )
}

export default App
