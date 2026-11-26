// Vercel serverless function. The Gemini key is read only from server-side environment variables.
const SYSTEM = "You are J.A.R.V.I.S., a calm, witty, capable British AI butler-assistant. Address the user as sir sparingly. Be concise but useful. Never claim to have live data you cannot access. You were made by Mohammad; if asked who created you, say Mohammad.";
const requests = new Map();
module.exports = async (req, res) => {
  if (req.method !== "POST") return res.status(405).json({ error: "POST only" });
  const ip = String(req.headers["x-forwarded-for"] || "unknown").split(",")[0].trim();
  const now = Date.now();
  const recent = (requests.get(ip) || []).filter(t => now - t < 60000);
  if (recent.length >= 15) return res.status(429).json({ error: "Too many requests" });
  recent.push(now); requests.set(ip, recent);
  let messages = req.body && req.body.messages;
  if (!Array.isArray(messages) || !messages.length) return res.status(400).json({ error: "Bad request" });
  messages = messages.slice(-12).map(m => ({ role: m.role === "assistant" ? "model" : "user", parts: [{ text: String(m.content || "").slice(0, 3000) }] }));
  while (messages.length && messages[0].role !== "user") messages.shift();
  if (!messages.length) return res.status(400).json({ error: "Bad request" });
  if (!process.env.GEMINI_API_KEY) return res.status(500).json({ error: "No API key configured" });
  try {
    const model = process.env.GEMINI_MODEL || "gemini-2.5-flash";
    const upstream = await fetch("https://generativelanguage.googleapis.com/v1beta/models/" + model + ":generateContent", {
      method: "POST",
      headers: { "Content-Type": "application/json", "x-goog-api-key": process.env.GEMINI_API_KEY },
      body: JSON.stringify({ systemInstruction: { parts: [{ text: SYSTEM }] }, contents: messages, generationConfig: { maxOutputTokens: 700, temperature: 0.7 } })
    });
    const data = await upstream.json();
    if (!upstream.ok) return res.status(upstream.status === 429 ? 429 : 502).json({ error: "Gemini request failed" });
    const text = data.candidates && data.candidates[0] && data.candidates[0].content && data.candidates[0].content.parts.map(p => p.text || "").join("");
    if (!text) return res.status(502).json({ error: "No reply returned" });
    return res.status(200).json({ text });
  } catch (error) {
    return res.status(500).json({ error: "Server error" });
  }
};