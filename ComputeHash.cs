using System;
using System.Security.Cryptography;
using System.Text;

var password = "mehak";
var bytes = Encoding.UTF8.GetBytes(password);
using var sha256 = SHA256.Create();
var hashBytes = sha256.ComputeHash(bytes);
var hash = BitConverter.ToString(hashBytes).Replace("-", "");
Console.WriteLine($"SHA256 hash of '{password}': {hash}");
