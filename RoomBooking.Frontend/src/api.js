const API_BASE_URL=import.meta.env.VITE_API_URL||'https://localhost:7000/api'; 
export async function apiRequest(path,options={}){const r=await fetch(`${API_BASE_URL}${path}`,{headers:{'Content-Type':'application/json',...(options.headers||{})},...options});if(!r.ok)throw Error(`API request failed: ${r.status}`);return r.status===204?null:r.json()}
