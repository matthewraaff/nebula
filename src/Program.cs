using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using NLua;

class Program
{
    static void Main(string[] args)
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 8080);
        listener.Start();
        Console.WriteLine("Server started on port 8080");

        while (true)
        {
            using (TcpClient client = listener.AcceptTcpClient())
            {
                NetworkStream stream = client.GetStream();

                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Received request: \n" + request);

                string response;
                if (request.EndsWith(".lua"))
                {
                    Console.WriteLine("Lua script moment?");
                    string scriptName = Path.GetFileName(request);
                    string scriptPath = Path.Combine("scripts", scriptName);

                    if (IsSafeScript(scriptPath))
                    {
                        Lua lua = new Lua();
                        lua.LoadCLRPackage();
                        lua["request"] = request;
                        lua["print"] = (Action<string>)Console.WriteLine;

                        lua.DoFile(scriptPath);
                        response = lua["response"].ToString();
                    }
                    else
                    {
                        response = "HTTP/1.1 404 Not Found\r\nContent-Type: text/plain\r\n\r\nFile not found";
                    }
                }
                else
                {
                    response = "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\nHello, world " + request + " !";
                }

                byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                stream.Write(responseBytes, 0, responseBytes.Length);
            }
        }


    }
    private static bool IsSafeScript(string scriptPath)
    {
        if (!Regex.IsMatch(scriptPath, @"^[^/]+\.lua$"))
        {
            Console.WriteLine("wtf?");
            return false;
        }

        string[] blacklistedScripts = { "uhoh.lua" };
        if (blacklistedScripts.Contains(scriptPath))
        {
            Console.WriteLine("User attempted to access blacklisted script: " + scriptPath);
            return false;
        }

        return true;
    }
}
