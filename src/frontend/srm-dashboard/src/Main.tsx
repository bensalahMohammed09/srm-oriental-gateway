import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App'
import './index.css'

/**
 * C'est ici que la magie commence. 
 * ReactDOM crée la racine de l'application et y attache notre composant <App />.
 * StrictMode est activé pour nous aider à détecter les pratiques déconseillées pendant le développement.
 */
ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
)