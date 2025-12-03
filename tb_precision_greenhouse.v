// tb_precision_greenhouse_fsm.v
module tb_precision_greenhouse_fsm;

    reg clk;
    reg reset_n;
    reg [5:0] S_in;
    wire [5:0] A_out;

    // Kabel Visual Baru
    wire SS0_IDLE;
    wire SS1_NUTRI;
    wire SS2_WATER;
    wire SS3_TEMP;
    wire SS4_SPRAY;
    wire SS5_FLUSH;
    wire SS6_GAS;
    wire SS7_CHAOS;

    precision_greenhouse_fsm DUT (
        .clk(clk),
        .reset_n(reset_n),
        .S(S_in),
        .A(A_out),
        .SS0_IDLE(SS0_IDLE),
        .SS1_NUTRI(SS1_NUTRI),
        .SS2_WATER(SS2_WATER),
        .SS3_TEMP(SS3_TEMP),
        .SS4_SPRAY(SS4_SPRAY),
        .SS5_FLUSH(SS5_FLUSH),
        .SS6_GAS(SS6_GAS),
        .SS7_CHAOS(SS7_CHAOS)
    );

    parameter PERIOD = 20;
    initial begin
        clk = 1'b0;
        forever #(PERIOD/2) clk = ~clk;
    end

    initial begin
        $dumpfile("simulasi_greenhouse.vcd");
        $dumpvars(0, tb_precision_greenhouse_fsm);

        // Reset
        reset_n = 1'b0; S_in = 6'b000000; #40;
        reset_n = 1'b1; #40;

        // --- SKENARIO MENYEBAR (SCATTERED) ---
        
        // 1. Mulai dengan Water sebentar
        S_in = 6'b000010; #100; 

        // 2. Tiba-tiba Suhu Panas (Water masih nyala -> Overlap)
        S_in = 6'b000110; #120;

        // 3. Water mati, ganti Spray (Overlap Temp + Spray)
        S_in = 6'b001100; #100;

        // 4. Jeda sebentar
        S_in = 6'b000000; #40;

        // 5. Nutrisi Masuk (Sendirian)
        S_in = 6'b000001; #80;

        // 6. Tiba-tiba butuh Flush
        S_in = 6'b010000; #100;

        // 7. Balik lagi ke Water (Menyebar kan?)
        S_in = 6'b000010; #120;

        // 8. Water + Nutri (Fertigasi)
        S_in = 6'b000011; #150;

        // 9. Jeda
        S_in = 6'b000000; #40;

        // 10. Gas Bocor (Panjang)
        S_in = 6'b100000; #200;

        // 11. Balik lagi ke Suhu Panas (Temp)
        S_in = 6'b000100; #100;

        // 12. Nutrisi lagi (Menyebar di akhir)
        S_in = 6'b000001; #80;

        // Selesai
        S_in = 6'b000000; #100;

        $finish;
    end
endmodule