using System;
using System.IO;

var cpu = new ZENITH_CPU();
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
    Console.WriteLine("program file not found.");
}

public class ZENITH_CPU
{
    public byte[] RAM = new byte[65536];
    public ushort[] Registers = new ushort[4];
    public ushort IP = 0;
    public bool IsRunning = true;
    public bool ZeroFlag = false;

    public void Step()
    {
        ushort inst = (ushort)((RAM[IP] << 8) | RAM[IP + 1]);

        int op = (inst >> 12) & 0x0F; // top 4 bits
        int reg = (inst >> 10) & 0x03; // next 2 bits
        int data = inst & 0x3FF; // last 10 bits

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

            case 0x3: // CMP
                // Compare Registers[reg] with Registers[data & 0x3] without modifying registers
                {
                    ushort a = Registers[reg];
                    ushort b = Registers[data & 0x3];
                    ZeroFlag = a == b;
                }
                break;

            case 0xA: // GRT  >
                {
                    ushort a = Registers[reg];
                    ushort b = Registers[data & 0x3];
                    ZeroFlag = a > b;
                }
                break;

            case 0xB: // GRE  >=
                {
                    ushort a = Registers[reg];
                    ushort b = Registers[data & 0x3];
                    ZeroFlag = a >= b;
                }
                break;

            case 0xC: // LES  <
                {
                    ushort a = Registers[reg];
                    ushort b = Registers[data & 0x3];
                    ZeroFlag = a < b;
                }
                break;

            case 0xD: // LEE  <=
                {
                    ushort a = Registers[reg];
                    ushort b = Registers[data & 0x3];
                    ZeroFlag = a <= b;
                }
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

            case 0x8: // JNZ
                if (!ZeroFlag)
                {
                    IP = (ushort)data;
                    return;
                }
                break;
            case 0x9: // INP
                Console.Write("? INPUT: ");
                if (ushort.TryParse(Console.ReadLine(), out ushort inputVal)) {
                   Registers[reg] = (ushort)(inputVal & 0xFFFF); // Store input in the target register
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
    public static void Load(ZENITH_CPU cpu, string[] lines)
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
                "CMP" => 0x3,
                "GRT" => 0xA,
                "GRE" => 0xB,
                "LES" => 0xC,
                "LEE" => 0xD,
                "SUB" => 0x5,
                "JMP" => 0x6,
                "JZ"  => 0x7,
                "JNZ" => 0x8,
                "INP" => 0x9,
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
