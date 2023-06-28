using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HexToString_Decryptor
{
    internal class Program
    {
        private static ModuleDefMD module;

        static void Main(string[] args)
        {
            module = ModuleDefMD.Load(args[0]);

            DecryptHex(module);

            var outputPath = Path.GetFileNameWithoutExtension(args[0]) + "_decrypted.exe";
            module.Write(outputPath, new ModuleWriterOptions(module)
            {
                Logger = DummyLogger.NoThrowInstance
            });
            Console.ReadKey();
        }
        public static string Hex2String(string mHex, object obj)
        {
            obj = 0;
            mHex = Regex.Replace(mHex, "[^0-9A-Fa-f]", "");
            if (mHex.Length % 2 != Convert.ToInt32(obj))
                mHex = mHex.Remove(mHex.Length - 1, 1);
            if (mHex.Length <= 0) return "";
            byte[] vBytes = new byte[mHex.Length / 2];
            for (int i = 0; i < mHex.Length; i += 2)
                if (!byte.TryParse(mHex.Substring(i, 2), NumberStyles.HexNumber, null, out vBytes[i / 2]))
                    vBytes[i / 2] = 0;
            return Encoding.Default.GetString(vBytes);
        }

        public static void DecryptHex(ModuleDefMD module)
        {
            int decryptedStringCount = 0;
            foreach (var typeDef in module.GetTypes())
                foreach (var methodDef in typeDef.Methods)
                {
                    if (!methodDef.HasBody) continue;
                    var instructions = new List<Instruction>(methodDef.Body.Instructions);

                    for (var i = instructions.Count - 1; i >= 0; i--)
                        if (instructions[i].OpCode.Code == Code.Ldstr &&
                            instructions[i + 1].OpCode.Code == Code.Ldnull &&
                            instructions[i + 2].OpCode.Code == Code.Call)
                        {
                            var String = (string)instructions[i].Operand;
                            var Object = (object)instructions[i + 4].Operand;
                            string DecryptedString = Hex2String(String, Object);
                            instructions[i].Operand = DecryptedString;

                            instructions[i + 1].OpCode = OpCodes.Nop;
                            instructions[i + 1].Operand = null;
                            instructions[i + 2].OpCode = OpCodes.Nop;
                            instructions[i + 2].Operand = null;

                            decryptedStringCount++;
                        }
                }
            /*            Console.ForegroundColor = ConsoleColor.Red;
                        Typewrite("\n\n# Finished Strings: " + decryptedStringCount + " #");*/

            Console.ForegroundColor = ConsoleColor.Red;
            if (decryptedStringCount != 0)
                Console.WriteLine($"\n\n       # Decrypted  {decryptedStringCount} Strings #");
        }
    }
}
