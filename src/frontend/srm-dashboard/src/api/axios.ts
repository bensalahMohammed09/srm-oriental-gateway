import axios from 'axios';

const api = axios.create({
  withCredentials: true, 
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
  },
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    // 📊 SRE: Formatage standardisé des logs d'erreur (Prêt pour ELK/Datadog)
    const logEvent = {
      timestamp: new Date().toISOString(),
      level: error.response?.status >= 500 ? "ERROR" : "WARN",
      event: "http_request_failed",
      service: "srm-frontend",
      request: {
        method: error.config?.method?.toUpperCase(),
        url: error.config?.url,
      },
      response: {
        status: error.response?.status,
        statusText: error.response?.statusText,
      },
      diagnostic: error.response?.status === 401 
        ? "Auth Token missing or invalid. Check Network tab > Request Headers to see if 'Cookie: SRM_AUTH_TOKEN' was sent." 
        : undefined
    };

    // Affiche le JSON pur dans la console
    console.log(JSON.stringify(logEvent));

    return Promise.reject(error);
  }
);

export default api;