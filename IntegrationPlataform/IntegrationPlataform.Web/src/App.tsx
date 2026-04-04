import { useEffect } from 'react';
import { AuthProvider, ThemeProvider, GlobalLoaderProvider, useGlobalLoader, setGlobalLoaderContext, Toaster, setApiBaseURL, setIdentityProviderURL } from 'd-rts';
import AppRoutes from './routes';

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL;
const identityProviderApiUrl = import.meta.env.VITE_IDENTITY_PROVIDER_API;

if (apiBaseUrl) {
  setApiBaseURL(apiBaseUrl);
}

if (identityProviderApiUrl) {
  setIdentityProviderURL(identityProviderApiUrl);
}

function AppContent() {
  const globalLoaderContext = useGlobalLoader();

  useEffect(() => {
    setGlobalLoaderContext(globalLoaderContext);
  }, [globalLoaderContext]);

  return (
    <AuthProvider>
      <AppRoutes />
      <Toaster />
    </AuthProvider>
  );
}

function App() {
  return (
    <ThemeProvider>
      <GlobalLoaderProvider>
        <AppContent />
      </GlobalLoaderProvider>
    </ThemeProvider>
  );
}

export default App;
