// precision_greenhouse_fsm.v
module precision_greenhouse_fsm (
    input  wire clk,
    input  wire reset_n,
    input  wire [5:0] S,       
    output reg  [5:0] A,
    
    // SS0_IDLE, SS1_NUTRI...SS7_CHAOS
    output wire SS0_IDLE,
    output wire SS1_NUTRI,
    output wire SS2_WATER,
    output wire SS3_TEMP,
    output wire SS4_SPRAY,
    output wire SS5_FLUSH,
    output wire SS6_GAS,
    output wire SS7_CHAOS
);

    // Parameter Internal
    parameter ST_IDLE      = 3'b000;
    parameter ST_NUTRI     = 3'b001;
    parameter ST_WATER     = 3'b010;
    parameter ST_TEMP      = 3'b011;
    parameter ST_SPRAY     = 3'b100;
    parameter ST_FLUSH     = 3'b101;
    parameter ST_GAS       = 3'b110;
    parameter ST_CHAOS     = 3'b111;

    reg [2:0] current_state;
    reg [2:0] next_state;

    // --- LOGIKA VISUALISASI ---
    assign SS0_IDLE    = 1'b0; 
    assign SS7_CHAOS   = 1'b0; 
    assign SS1_NUTRI   = S[0]; 
    assign SS2_WATER   = S[1]; 
    assign SS3_TEMP    = S[2]; 
    assign SS4_SPRAY   = S[3];
    assign SS5_FLUSH   = S[4];
    assign SS6_GAS     = S[5];

    // Update State
    always @(posedge clk or negedge reset_n) begin
        if (!reset_n) current_state <= ST_IDLE;
        else          current_state <= next_state;
    end

    // Next State Logic
    always @(*) begin
        next_state = current_state;
        if (S[5] == 1'b1)        next_state = ST_GAS;   
        else if (S[4] == 1'b1)   next_state = ST_FLUSH;
        else if (S[3] == 1'b1)   next_state = ST_SPRAY;
        else if (S[2] == 1'b1)   next_state = ST_TEMP;
        else if (S[1] == 1'b1)   next_state = ST_WATER;
        else if (S[0] == 1'b1)   next_state = ST_NUTRI;
        else                     next_state = ST_IDLE;
        
        if (current_state == ST_GAS && S[5] == 0) next_state = ST_IDLE;
    end

    // Output Aktuator
    always @(current_state) begin
        case (current_state)
            ST_NUTRI: A = 6'b000001;
            ST_WATER: A = 6'b000010;
            ST_TEMP:  A = 6'b000100;
            ST_SPRAY: A = 6'b001000;
            ST_FLUSH: A = 6'b010000;
            ST_GAS:   A = 6'b100000;
            default:  A = 6'b000000;
        endcase
    end

endmodule
