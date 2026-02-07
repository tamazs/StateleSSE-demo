const local = "http://localhost:5026";
const prod = "https://chatdemo.fly.dev/"
const isProd = import.meta.env.PROD

export const BASE_URL = isProd ? prod : local;