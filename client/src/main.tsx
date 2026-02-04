import {createRoot} from 'react-dom/client'
import App from './App.tsx'
import {StreamProvider} from "./useStream.tsx";

createRoot(document.getElementById('root')!).render(
    <StreamProvider config={{
        connectEvent: 'ConnectionResponse',
        urlForStreamEndpoint: 'http://localhost:5026/api/realtime/connect'
    }}>
        <App/>
    </StreamProvider>,
)