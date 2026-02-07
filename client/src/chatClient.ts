import {RealtimeClient} from "./generated-ts-client.ts";
import {BASE_URL} from "./utils/BASE_URL.ts";
import {customFetch} from "./utils/customFetch.ts";

export const chatClient = new RealtimeClient(BASE_URL, customFetch);