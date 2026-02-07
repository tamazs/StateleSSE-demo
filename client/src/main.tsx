import {createRoot} from 'react-dom/client'
import {StreamProvider} from "./useStream.tsx";
import {BASE_URL} from "./utils/BASE_URL.ts";
import Routes from "./Routes.tsx";
import './styles.css';

createRoot(document.getElementById('root')!).render(
    <StreamProvider config={{
        urlForStreamEndpoint: `${BASE_URL}/api/realtime/connect`,
        connectEvent: "ConnectionResponse",
    }}>
        <Routes/>
    </StreamProvider>
)