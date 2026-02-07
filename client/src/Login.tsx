import {authClient} from "./authClient.ts";
import {useState} from "react";
import type {RegisterLoginDto, LoginUserDto} from "./generated-ts-client.ts";

export default function Login() {

    const [authForm, setAuthForm] = useState<RegisterLoginDto>({
        password: "pass",
        userName: "test"
    })

    return <div className="login-section">
        <input
            className="input"
            placeholder="username"
            value={authForm.userName}
            onChange={e => setAuthForm({
                ...authForm, userName : e.target.value
            })}
        />
        <input
            className="input"
            placeholder="password"
            value={authForm.password}
            type="password"
            onChange={e => setAuthForm({
                ...authForm, password : e.target.value
            })}
        />
        <div className="auth-buttons">
            <button
                className="btn btn-primary"
                onClick={() => {
                    authClient.loginUser(authForm).then(r => {
                        alert('welcome!')
                        console.log(r);
                        localStorage.setItem('jwt', r.token!)
                    })
                }}
            >
                Login
            </button>
            <button
                className="btn btn-secondary"
                onClick={() => {
                    authClient.registerUser(authForm).then(r => {
                        alert('registered!')
                    })
                }}
            >
                Register
            </button>
        </div>
    </div>
}