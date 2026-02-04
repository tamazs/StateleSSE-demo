import {useStream} from "./useStream.tsx";
import {useEffect} from "react";
import type {MessageResponseDto} from "./generated-ts-client.ts";

function App() {

  const stream = useStream();
  const connectionId = stream.connectionId;

  useEffect(() => {
    stream.on<MessageResponseDto>('randomroom', 'MessageResponseDto', (dto) => {
      console.log(dto);
      alert("Someone broadcasted in a room you were in")
    })
  }, [])

  return (
    <>
      <button
          onClick={() => {
            fetch('http://localhost:5026/api/realtime/join', {
              method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
              body: JSON.stringify({
                connectionId: connectionId,
                group: "randomroom"
              })
            })
          }}>JOIN ROOM</button>
        <button
            onClick={() => {
                fetch('http://localhost:5026/api/realtime/send', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({
                        groupId: "54b14451-e0e8-4c01-b690-a0aa7e68567c",
                        message: "hello from client button"
                    })
                })
            }}>SEND MESSAGE</button>
    </>
  )
}

export default App
