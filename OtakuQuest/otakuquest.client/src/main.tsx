import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { OpenAPI } from './api/generated/index.ts'

//OpenAPI.BASE = 'https://localhost:7048';
OpenAPI.BASE = import.meta.env.VITE_API_BASE_URL;

OpenAPI.TOKEN = async () => {
  return localStorage.getItem('token') || '';
};

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
