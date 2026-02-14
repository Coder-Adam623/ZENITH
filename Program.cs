using System;
using System.IO;

var cpu = new ZENITHCPU();
string filePath = "main.zen";

if (File.Exists(filePath))
{
    string[] programLines = File.ReadAllLines(filePath);
    ZENITHAssembler.Load(cpu, programLines);

    while (cpu.IsRunning)
        cpu.Step();
}
else
{
    Console.WriteLine("main.zen not found.");
}

public class ZENITHCPU
{
    public byte[] RAM = new byte[65536];
    public ushort[] Registers = new ushort[4];
    public ushort IP = 0;
    public bool IsRunning = true;
    public bool ZeroFlag = false;

    public void Step()
    {
        ushort inst = (ushort)((RAM[IP] << 8) | RAM[IP + 1]);

        int op = (inst >> 12) & 0x0F;
        int reg = (inst >> 10) & 0x03;
        int data = inst & 0x3FF;

        switch (op)
        {
            case 0x0: // HLT
                IsRunning = false;
                break;

            case 0x1: // LDI
                Registers[reg] = (ushort)data;
                break;

            case 0x2: // ADD
                Registers[reg] += Registers[data & 0x3];
                ZeroFlag = Registers[reg] == 0;
                break;

            case 0x5: // SUB
                Registers[reg] -= Registers[data & 0x3];
                ZeroFlag = Registers[reg] == 0;
                break;

            case 0x6: // JMP
                IP = (ushort)data;
                return;

            case 0x7: // JZ
                if (ZeroFlag)
                {
                    IP = (ushort)data;
                    return;
                }
                break;

            case 0xE: // OUT
                Console.WriteLine($"> OUT: {Registers[reg]}");
                break;

            default:
                Console.WriteLine($"Unknown opcode {op:X}");
                IsRunning = false;
                break;
        }

        IP += 2;
    }
}

public static class ZENITHAssembler
{
    public static void Load(ZENITHCPU cpu, string[] lines)
    {
        ushort addr = 0;

        foreach (var raw in lines)
        {
            string l = raw.Split(';')[0].Trim();

            if (string.IsNullOrWhiteSpace(l))
                continue;

            var p = l.Replace(",", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries);

            string opToken = p[0].Trim().ToUpper();
            byte op = opToken switch
            {
                "HLT" => 0x0,
                "LDI" => 0x1,
                "ADD" => 0x2,
                "SUB" => 0x5,
                "JMP" => 0x6,
                "JZ"  => 0x7,
                "OUT" => 0xE,
                _ => throw new Exception($"Unknown instruction: {opToken}")
            };

            byte reg = 0;
            ushort data = 0;

            if (p.Length > 1)
            {
                if (p[1].StartsWith("R", StringComparison.OrdinalIgnoreCase))
                    reg = (byte)(int.Parse(p[1].Substring(1)) & 0x03);
                else
                    data = (ushort)(int.Parse(p[1]) & 0x3FF);
            }

            if (p.Length > 2)
            {
                if (p[2].StartsWith("R", StringComparison.OrdinalIgnoreCase))
                    data = (ushort)(int.Parse(p[2].Substring(1)) & 0x03);
                else
                    data = (ushort)(int.Parse(p[2]) & 0x3FF);
            }

            ushort packed = (ushort)((op << 12) | (reg << 10) | (data & 0x3FF));

            cpu.RAM[addr++] = (byte)(packed >> 8);
            cpu.RAM[addr++] = (byte)(packed & 0xFF);
        }
    }
}
