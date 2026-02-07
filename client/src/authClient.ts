import {AuthClient} from "./generated-ts-client.ts";
import {BASE_URL} from "./utils/BASE_URL.ts";
import {customFetch} from "./utils/customFetch.ts";

export const authClient = new AuthClient(BASE_URL, customFetch);