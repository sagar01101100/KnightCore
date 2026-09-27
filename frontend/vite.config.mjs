import {defineConfig} from 'vite';
// Managed preview serves the compiled Angular application. npm start uses ng serve.
export default defineConfig({root:'dist/browser',server:{host:'0.0.0.0',allowedHosts:['terminal.local'],proxy:{'/api':{target:'http://127.0.0.1:5080'}}}});
