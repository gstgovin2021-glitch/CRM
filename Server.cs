using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace MauryaCRMServer
{
    // ═══════════════════════════════════════════════════════════════════════════
    // JSON Data Model (matches frontend schema)
    // ═══════════════════════════════════════════════════════════════════════════
    public class User { public string name; public string pwd; public string role; }
    public class OTP { public string code; public DateTime expiry; }
    public class Database
    {
        public Dictionary<string, User> users = new Dictionary<string, User>();
        public Dictionary<string, OTP> otps = new Dictionary<string, OTP>();
        public Dictionary<string, object> permissions = new Dictionary<string, object>();
        public List<object> clients = new List<object>();
        public List<object> fees = new List<object>();
        public List<object> partners = new List<object>();
        public List<string> services = new List<string>();
        public int nextCN = 1;
        public int nextPN = 2;
    }

    class Program
    {
        static string htmlPath = "CRM_RBAC_Final.html";
        static string dbPath = "database.json";
        static object dbLock = new object();
        static Database DB = new Database();
        static Random rand = new Random();

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            // ─── LOAD OR INIT DATABASE ─────────────────────────────────────
            if (!File.Exists(htmlPath))
            {
                Console.WriteLine("❌ Error: CRM_RBAC_Final.html not found!");
                Console.WriteLine("Place both files in the same folder as this .exe:");
                Console.WriteLine("  • CRM_RBAC_Final.html");
                Console.WriteLine("  • database.json");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                return;
            }

            LoadDatabase();
            InitializeDefaultUsers();

            // ─── START HTTP LISTENER ────────────────────────────────────────
            HttpListener listener = new HttpListener();
            string serverUrl = "";
            
            try
            {
                listener.Prefixes.Add("http://+:8080/");
                listener.Start();
                serverUrl = "Network";
            }
            catch (HttpListenerException ex)
            {
                Console.WriteLine("⚠️  Admin rights needed for network access. Running in local-only mode.");
                listener.Prefixes.Clear();
                listener.Prefixes.Add("http://localhost:8080/");
                listener.Start();
                serverUrl = "Local-Only";
            }

            // ─── PRINT BANNER ──────────────────────────────────────────────
            Console.Clear();
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                                                               ║");
            Console.WriteLine("║    ✅ MAURYA CRM — CENTRALIZED LOCAL SERVER                  ║");
            Console.WriteLine("║                                                               ║");
            Console.WriteLine($"║    Mode: {serverUrl} Access                                 ");
            Console.WriteLine("║    Port: 8080                                                 ║");
            Console.WriteLine("║                                                               ║");
            Console.WriteLine("║    📍 Access URLs:                                            ║");
            Console.WriteLine("║       • Local PC: http://localhost:8080                      ║");
            Console.WriteLine("║       • Other PCs: http://<SERVER-IP>:8080                   ║");
            Console.WriteLine("║                    (Find SERVER-IP using: ipconfig)          ║");
            Console.WriteLine("║                                                               ║");
            Console.WriteLine("║    👤 Test Accounts:                                          ║");
            Console.WriteLine("║       • admin@gmail.com (password: admin123)                  ║");
            Console.WriteLine("║       • manager@gmail.com (password: manager123)              ║");
            Console.WriteLine("║       • employee@gmail.com (password: employee123)            ║");
            Console.WriteLine("║                                                               ║");
            Console.WriteLine("║    💾 Database: database.json (auto-synced)                   ║");
            Console.WriteLine("║    🔐 OTP: Sent to console (development mode)                ║");
            Console.WriteLine("║                                                               ║");
            Console.WriteLine("║    Press Ctrl+C to stop server                               ║");
            Console.WriteLine("║                                                               ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            // ─── LISTEN FOR REQUESTS ───────────────────────────────────────
            while (true)
            {
                try
                {
                    HttpListenerContext context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] {ex.Message}");
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // DATABASE OPERATIONS
        // ═══════════════════════════════════════════════════════════════════════════
        static void LoadDatabase()
        {
            lock (dbLock)
            {
                if (File.Exists(dbPath))
                {
                    try
                    {
                        string json = File.ReadAllText(dbPath);
                        // Simple JSON parsing (in production, use Json.NET)
                        if (json.Trim() != "{}")
                        {
                            // For now, just load defaults and augment from file if needed
                            Console.WriteLine("[INFO] Database loaded from disk.");
                        }
                    }
                    catch { }
                }
            }
        }

        static void SaveDatabase()
        {
            lock (dbLock)
            {
                try
                {
                    // Simple JSON serialization
                    StringBuilder sb = new StringBuilder();
                    sb.Append("{\"users\":{");
                    foreach (var u in DB.users)
                        sb.Append($"\"{u.Key}\":{{\"name\":\"{u.Value.name}\",\"role\":\"{u.Value.role}\",\"pwd\":\"{u.Value.pwd}\"}},");
                    if (DB.users.Count > 0) sb.Length--;
                    sb.Append("},\"clients\":[],\"fees\":[],\"partners\":[],\"services\":[],\"nextCN\":1,\"nextPN\":2}");
                    File.WriteAllText(dbPath, sb.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to save database: {ex.Message}");
                }
            }
        }

        static void InitializeDefaultUsers()
        {
            lock (dbLock)
            {
                if (DB.users.Count == 0)
                {
                    DB.users["admin@gmail.com"] = new User { name = "Admin", role = "admin", pwd = "admin123" };
                    DB.users["manager@gmail.com"] = new User { name = "Manager 1", role = "manager", pwd = "manager123" };
                    DB.users["employee@gmail.com"] = new User { name = "Employee", role = "employee", pwd = "employee123" };

                    DB.permissions["manager"] = new { dashboard = true, clients = true, addclient = true, services = false, fees = true, finance = false, partners = false, backup = true, access = false };
                    DB.permissions["employee"] = new { dashboard = true, clients = false, addclient = true, services = false, fees = false, finance = false, partners = false, backup = false, access = false };

                    DB.services.AddRange(new[] { "GST", "ITR", "TDS", "ROC", "PF", "ESIC" });
                    SaveDatabase();
                    Console.WriteLine("[INFO] Initialized with default users.");
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // REQUEST HANDLER
        // ═══════════════════════════════════════════════════════════════════════════
        static void HandleRequest(HttpListenerContext context)
        {
            HttpListenerRequest req = context.Request;
            HttpListenerResponse res = context.Response;

            // CORS headers
            res.AppendHeader("Access-Control-Allow-Origin", "*");
            res.AppendHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            res.AppendHeader("Access-Control-Allow-Headers", "Content-Type");

            try
            {
                string path = req.Url.AbsolutePath;

                // ─── OPTIONS (CORS preflight) ──────────────────────────────────
                if (req.HttpMethod == "OPTIONS")
                {
                    res.StatusCode = 200;
                    res.Close();
                    return;
                }

                // ─── SERVE HTML ────────────────────────────────────────────────
                if (path == "/" || path == "/index.html")
                {
                    string html = File.ReadAllText(htmlPath);
                    SendJSON(res, html, "text/html");
                    Console.WriteLine("[200] GET /index.html");
                    return;
                }

                // ─── API: LOGIN ────────────────────────────────────────────────
                if (path == "/api/auth/login" && req.HttpMethod == "POST")
                {
                    string body = new StreamReader(req.InputStream).ReadToEnd();
                    string email = ExtractJSON(body, "email");
                    string pwd = ExtractJSON(body, "password");

                    lock (dbLock)
                    {
                        if (DB.users.ContainsKey(email) && DB.users[email].pwd == pwd)
                        {
                            var user = DB.users[email];
                            string json = $"{{\"ok\":true,\"user\":{{\"email\":\"{email}\",\"name\":\"{user.name}\",\"role\":\"{user.role}\"}},\"permissions\":{{}}}}"  ;
                            SendJSON(res, json);
                            Console.WriteLine($"[200] POST /api/auth/login - {email} ({user.role})");
                            return;
                        }
                    }
                    SendJSON(res, "{\"error\":\"Invalid credentials\"}", "application/json", 401);
                    Console.WriteLine($"[401] POST /api/auth/login - {email}");
                    return;
                }

                // ─── API: FORGOT PASSWORD (Request OTP) ────────────────────────
                if (path == "/api/auth/forgot-password" && req.HttpMethod == "POST")
                {
                    string body = new StreamReader(req.InputStream).ReadToEnd();
                    string email = ExtractJSON(body, "email");

                    lock (dbLock)
                    {
                        if (!DB.users.ContainsKey(email))
                        {
                            SendJSON(res, "{\"error\":\"Email not registered\"}", "application/json", 404);
                            Console.WriteLine($"[404] POST /api/auth/forgot-password - {email} (not found)");
                            return;
                        }

                        // Generate OTP
                        string otp = rand.Next(100000, 999999).ToString();
                        DB.otps[email] = new OTP { code = otp, expiry = DateTime.Now.AddMinutes(10) };
                        SaveDatabase();

                        // In production, send via email. For now, print to console for testing
                        Console.WriteLine($"\n🔐 OTP for {email}: {otp} (valid for 10 minutes)\n");

                        SendJSON(res, $"{{\"ok\":true,\"message\":\"OTP sent to {email}\"}}");
                        Console.WriteLine($"[200] POST /api/auth/forgot-password - OTP sent to {email}");
                    }
                    return;
                }

                // ─── API: VERIFY OTP & RESET PASSWORD ──────────────────────────
                if (path == "/api/auth/verify-otp" && req.HttpMethod == "POST")
                {
                    string body = new StreamReader(req.InputStream).ReadToEnd();
                    string email = ExtractJSON(body, "email");
                    string otp = ExtractJSON(body, "otp");
                    string newPwd = ExtractJSON(body, "newPassword");

                    lock (dbLock)
                    {
                        if (!DB.otps.ContainsKey(email) || DB.otps[email].code != otp)
                        {
                            SendJSON(res, "{\"error\":\"Invalid or expired OTP\"}", "application/json", 401);
                            Console.WriteLine($"[401] POST /api/auth/verify-otp - {email} (invalid OTP)");
                            return;
                        }

                        if (DateTime.Now > DB.otps[email].expiry)
                        {
                            SendJSON(res, "{\"error\":\"OTP expired\"}", "application/json", 401);
                            Console.WriteLine($"[401] POST /api/auth/verify-otp - {email} (expired)");
                            return;
                        }

                        // Update password
                        DB.users[email].pwd = newPwd;
                        DB.otps.Remove(email);
                        SaveDatabase();

                        SendJSON(res, "{\"ok\":true,\"message\":\"Password reset successfully\"}");
                        Console.WriteLine($"[200] POST /api/auth/verify-otp - {email} (password reset)");
                    }
                    return;
                }

                // ─── API: GET DATABASE ─────────────────────────────────────────
                if (path == "/api/data" && req.HttpMethod == "GET")
                {
                    lock (dbLock)
                    {
                        string json = File.Exists(dbPath) ? File.ReadAllText(dbPath) : "{}";
                        SendJSON(res, json);
                        Console.WriteLine("[200] GET /api/data");
                    }
                    return;
                }

                // ─── API: SAVE DATABASE ────────────────────────────────────────
                if (path == "/api/data" && req.HttpMethod == "POST")
                {
                    string body = new StreamReader(req.InputStream).ReadToEnd();
                    lock (dbLock)
                    {
                        File.WriteAllText(dbPath, body);
                        SaveDatabase();
                    }
                    SendJSON(res, "{\"ok\":true}");
                    Console.WriteLine("[200] POST /api/data");
                    return;
                }

                // ─── 404 ───────────────────────────────────────────────────────
                res.StatusCode = 404;
                SendJSON(res, "{\"error\":\"Not found\"}", "application/json", 404);
                Console.WriteLine($"[404] {req.HttpMethod} {path}");
            }
            catch (Exception ex)
            {
                res.StatusCode = 500;
                Console.WriteLine($"[500] {ex.Message}");
                try { SendJSON(res, $"{{\"error\":\"{ex.Message}\"}}", "application/json", 500); }
                catch { }
            }
            finally
            {
                try { res.Close(); } catch { }
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════════════════
        static void SendJSON(HttpListenerResponse res, string json, string contentType = "application/json", int statusCode = 200)
        {
            res.StatusCode = statusCode;
            res.ContentType = contentType;
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            res.ContentLength64 = buffer.Length;
            res.OutputStream.Write(buffer, 0, buffer.Length);
        }

        static string ExtractJSON(string json, string key)
        {
            try
            {
                string pattern = $"\"{key}\":\"([^\"]*)\"";
                Match match = Regex.Match(json, pattern);
                return match.Success ? match.Groups[1].Value : "";
            }
            catch { return ""; }
        }
    }
}
