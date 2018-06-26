using System;

public class Z80 {

    public enum Register
    {
        A = 0,
        F = 1,
        BC = 2,
        B = 2,
        C = 3,
        DE = 4,
        D = 4,
        E = 5,
        HL = 6,
        H = 6,
        L = 7,
        AD = 8,
        FD = 9,
        BD = 10,
        CD = 11,
        DD = 12,
        ED = 13,
        HD = 14,
        LD = 15,
        I = 16,
        R = 17,
        IX = 18,
        IXH = 18,
        IXL = 19,
        IY = 20,
        IYH = 20,
        IYL = 21,
        SP = 22,
        SPH = 22,
        SPL = 23,
        PC = 24,
        PCH = 24,
        PCL = 25,
        MAX = 26
    }

    public enum Flag
    {
        C = 0,  // Carry
        N = 1,  // Subtract
        PV = 2, // Parity / Overflow
        F3 = 3, // undocumented
        H = 4,  // Half Carry
        F5 = 5, // undocumented
        Z = 6,  // Zero
        S = 7   // Sign
    }

    public byte[] registers = new byte[(int)Register.MAX];
    public byte[] memory;

    /// <summary>
    /// DDD or SSS
    /// </summary>
    public int[] register_pattern = new int[] {
        (int)Register.B, // 000
        (int)Register.C, // 001
        (int)Register.D, // 010
        (int)Register.E, // 011
        (int)Register.H, // 100
        (int)Register.L, // 101
        -1,              // 110 (HL)
        (int)Register.A  // 111
    };

    /// <summary>
    /// Register pair
    /// </summary>
    public int[] register_pair = new int[]
    {
        (int)Register.BC, // 00
        (int)Register.DE, // 01
        (int)Register.HL, // 10
        (int)Register.SP  // 11
    };

    public bool halt = false;
    public bool swap = false;

    /// <summary>
    /// Constructer
    /// </summary>
    /// <param name="memorysize"></param>
    public Z80(UInt16 memorysize = 65535)
    {
        memory = new byte[memorysize];
        for (int ii=0; ii<(int)Register.MAX; ii++)
        {
            registers[ii] = 0;
        }
        for (int ii=0; ii<memorysize; ii++)
        {
            memory[ii] = 0;
        }
    }

    /// <summary>
    /// opcode fetch
    /// </summary>
    public void Fetch()
    {
        // HALT
        if (halt)
            return;

        // Fetch
        var pc = (registers[(int)Register.PCH] << 8) + registers[(int)Register.PCL];
        var opcode = memory[pc];
        var category = opcode & 0xA0;

        // Register (8bit)
        var offset = swap ? (int)Register.AD : (int)Register.A;
        var a = registers[(int)Register.A + offset];
        var b = registers[(int)Register.B + offset];
        var c = registers[(int)Register.C + offset];
        var d = registers[(int)Register.D + offset];
        var e = registers[(int)Register.E + offset];
        var h = registers[(int)Register.H + offset];
        var l = registers[(int)Register.L + offset];
        // Register (16bit)
        var bc = (b << 8) + c;
        var de = (d << 8) + e;
        var hl = (h << 8) + l;
        var sp = (registers[(int)Register.SPH] << 8) + registers[(int)Register.SPL];
        var ix = (registers[(int)Register.IXH] << 8) + registers[(int)Register.IXL];
        var iy = (registers[(int)Register.IYH] << 8) + registers[(int)Register.IYL];
        // Address op ll hh
        var addr = (memory[pc + 2] << 8) + memory[pc + 1];
        // data
        var d1 = memory[pc + 1];
        var d2 = memory[pc + 2];
        var d3 = memory[pc + 3];
        // ??DDDSSS
        var ddd = (opcode & 0X38) >> 3;
        var sss = (opcode & 0x07);
        var r1 = register_pattern[ddd];
        var r2 = register_pattern[sss];
        // system
        var step = 1;

        // Machine cycle
        switch (category)
        {
            case 0x00:
                {
                    switch (opcode)
                    {
                        case 0x00: // 00 000 000 -------- -------- NOP
                        case 0x01: // LD BC, n'n
                            break;
                        case 0x02: // 00 000 010 -------- -------- (BC) <- A
                            memory[bc] = a;
                            break;
                        case 0x03: // INC BC
                        case 0x04: // INC B
                        case 0x05: // DEC B
                        case 0x07: // RLCA
                        case 0x08: // EX AF,AF'
                        case 0x09: // ADD HL, BC
                            break;
                        case 0x0A: // 00 001 010 -------- -------- A    <- (BC)
                            registers[(int)Register.A + offset] = memory[bc];
                            break;
                        case 0x0B: // DEC BC
                        case 0x0C: // INC C
                        case 0x0D: // DEC C
                        case 0x0F: // RRCA
                        case 0x10: // DJNZ
                        case 0x11: // LD DE, n'n
                            break;
                        case 0x12: // 00 010 010 -------- -------- (DE) <- A
                            memory[de] = a;
                            break;
                        case 0x13: // INC DE
                        case 0x14: // INC D
                        case 0x15: // DEC D
                        case 0x17: // RLA
                        case 0x18: // JR e
                        case 0x19: // ADD HL, DE
                            break;
                        case 0x1A: // 00 011 010 -------- -------- A    <- (DE)
                            registers[(int)Register.A + offset] = memory[de];
                            break;
                        case 0x1B: // DEC DE
                        case 0x1C: // INC E

                            break;
                        case 0x32: // 00 110 010 llllllll hhhhhhhh (ad) <- A
                            memory[addr] = a;
                            step = 3;
                            break;
                        case 0x3A: // 00 111 010 llllllll hhhhhhhh A    <- (ad)
                            registers[(int)Register.A + offset] = memory[addr];
                            step = 3;
                            break;
                        case 0x36: // 00 110 110 nnnnnnnn -------- (HL) <- n
                            memory[hl] = d1;
                            step = 2;
                            break;
                        case 0x06: // 00 DDD 110 nnnnnnnn -------- DDD  <- n
                        case 0x0E:
                        case 0x16:
                        case 0x1E:
                        case 0x26:
                        case 0x2E:
                        case 0x3E:
                            registers[r1 + offset] = d1;
                            step = 2;
                            break;
                    }
                }
                break;
            case 0x40: // 01DDDSSS
                {
                    // 01110110 HALT
                    if (opcode == 0x76)
                    {
                        halt = true;
                        break;
                    }
                    // 01DDDSSS LD
                    var source = (r2 == -1) ? memory[hl] : registers[r2 + offset];
                    if (r1 == -1)
                    {
                        memory[hl] = source;
                    } else
                    {
                        registers[r1 + offset] = source;
                    }
                }
                break;
            case 0x80:
                break;
            case 0xC0: // 11??????
                {
                    // 11011101 01DDD110 dddddddd -------- DDD      <- (IX + d)
                    // 11111101 01DDD110 dddddddd -------- DDD      <- (IY + d)
                    // 11011101 01110SSS dddddddd -------- (IX + d) <- SSS
                    // 11111101 01110SSS dddddddd -------- (IY + d) <- SSS
                    // 11011101 00110110 dddddddd nnnnnnnn (IX + d) <- n
                    // 11111101 00110110 dddddddd nnnnnnnn (IY + d) <- n
                    // 11101101 01010111 -------- -------- A        <- I
                    // 11101101 01000111 -------- -------- I        <- A
                    // 11101101 01011111 -------- -------- A        <- R
                    // 11101101 01001111 -------- -------- R        <- A
                }
                break;
        }
        // Increment PC
        pc += step;
        registers[(int)Register.PCH] = (byte)((pc & 0xFF00) >> 8);
        registers[(int)Register.PCL] = (byte)(pc & 0xFF);
    }

}
