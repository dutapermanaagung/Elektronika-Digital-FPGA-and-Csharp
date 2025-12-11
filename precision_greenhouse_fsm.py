import random
import time
from qiskit import QuantumCircuit, transpile
from qiskit_aer import AerSimulator

class QuantumGreenhouseController:
    def __init__(self):
        self.simulator = AerSimulator()
        self.states = {
            0: "SS0_IDLE",
            1: "SS1_NUTRI",
            2: "SS2_WATER",
            3: "SS3_TEMP",
            4: "SS4_SPRAY",
            5: "SS5_FLUSH",
            6: "SS6_GAS",
            7: "SS7_SHUTDOWN"
        }

    def run_logic(self, s1, s2, s3, s4, s5, s6):
        """
        Membangun dan menjalankan sirkuit quantum.
        Returns: (state_idx, state_name, aksi, qc_object, raw_counts)
        """
   
        qc = QuantumCircuit(9, 3)

        inputs = [s1, s2, s3, s4, s5, s6]
        for i, is_active in enumerate(inputs):
            if is_active:
                qc.x(i)

        qc.barrier()
        
        qc.mcx([0, 1, 2, 3, 4, 5], 6, ctrl_state='000001')
        qc.mcx([1, 2, 3, 4, 5], 7, ctrl_state='00001')
        qc.mcx([2, 3, 4, 5], 6, ctrl_state='0001')
        qc.mcx([2, 3, 4, 5], 7, ctrl_state='0001')
        qc.mcx([3, 4, 5], 8, ctrl_state='001')
        qc.mcx([4, 5], 6, ctrl_state='01')
        qc.mcx([4, 5], 8, ctrl_state='01')
        qc.mcx([5], 7, ctrl_state='1')
        qc.mcx([5], 8, ctrl_state='1')                    
        qc.mcx([0, 1, 2, 3, 4, 5], 6, ctrl_state='111111') 
        
        qc.barrier()
        
        qc.measure([6, 7, 8], [0, 1, 2])

        compiled_circuit = transpile(qc, self.simulator)
        job = self.simulator.run(compiled_circuit, shots=1024)
        result = job.result()
        counts = result.get_counts()
        
        dominant_bin = max(counts, key=counts.get)
        state_idx = int(dominant_bin, 2)
        
        return self.get_result_details(state_idx, qc, counts)

    def get_result_details(self, state_idx, qc, counts):
        state_name = self.states.get(state_idx, "UNKNOWN")
        
        aksi = "Standby (Monitoring)"
        if state_idx == 1: aksi = "Menyuntikkan Pupuk (Injector ON)"
        elif state_idx == 2: aksi = "Menyalakan Pompa Air (Pump ON)"
        elif state_idx == 3: aksi = "Menyalakan Kipas Exhaust (Fan ON)"
        elif state_idx == 4: aksi = "Menyalakan Sprinkler (Sprayer ON)"
        elif state_idx == 5: aksi = "Membuka Valve Flushing (Valve ON)"
        elif state_idx == 6: aksi = "Menyemprot Anti-Etilen (1-MCP ON)"
        elif state_idx == 7: aksi = "CRITICAL FAILURE: EMERGENCY SHUTDOWN!!"
        
        return state_idx, state_name, aksi, qc, counts

def draw_text_histogram(counts, total_shots=1024):
    """
    Fungsi visualisasi histogram berbasis teks untuk terminal.
    Ini mensimulasikan tampilan probabilitas state quantum.
    """
    print("\n   [Quantum State Distribution Histogram]")
    print("   " + "-"*45)

    sorted_keys = sorted(counts.keys())
    
    for state in sorted_keys:
        count = counts[state]
        percentage = (count / total_shots) * 100
        bar_length = int(percentage / 2.5)
        bar = "█" * bar_length
        print(f"   |{state}⟩ : {bar:<40} {count} shots ({percentage:.1f}%)")
    print("   " + "-"*45)

def main():
    ctrl = QuantumGreenhouseController()
    
    print("\n" + "="*85)
    print("   GREENHOUSE QUANTUM FSM SIMULATION (QISKIT AER)   ")
    print("   Backend: aer_simulator | Shots: 1024             ")
    print("="*85)
    
    base_priorities = [0.1, 0.3, 0.45, 0.60, 0.75, 1.0]

    while True:
        target_scenario = random.randint(0, 8)
 
        v1, v2, v3 = 4.0, 400.0, 28.0
        v4, v5, v6 = 0.8, 6.0, 5.0
        
        if target_scenario == 1: v1 = 3.6
        elif target_scenario == 2: v2 = 800
        elif target_scenario == 3: 
            v3 = 35.0; 
            if random.choice([True, False]): v2 = 900
        elif target_scenario == 4:
            v4 = 0.2
            if random.choice([True, False]): v3 = 33.0
        elif target_scenario == 5: v5 = 2.0
        elif target_scenario == 6: 
            v6 = 18.0; v1 = 3.6; v3 = 38.0
        elif target_scenario == 7:
            v1 = 3.6; v2 = 900; v3 = 45.0; v4 = 0.1; v5 = 2.5; v6 = 20.0
            
        v1 += (random.random() * 0.1) - 0.05
        v3 += (random.random() * 1.0) - 0.5
        
        s_active = [False] * 6
        s_active[0] = v1 < 3.8                   # S1 NPK
        s_active[1] = 600 < v2 < 1000            # S2 Water
        s_active[2] = v3 > 30.0                  # S3 Temp
        s_active[3] = v4 < 0.3                   # S4 NDVI
        s_active[4] = 0.5 <= v5 <= 3.9           # S5 EC
        s_active[5] = v6 >= 14.0                 # S6 Gas

        # Bobot Probabilitas
        weights = []
        total_weight = 0.0
        for i in range(6):
            noise = random.random() * 0.1
            w = (base_priorities[i] * 50.0) + noise if s_active[i] else (random.random() * 0.5)
            weights.append(w)
            total_weight += w
            
        final_probs = []
        check_sum = 0.0
        for w in weights:
            p = w / total_weight if total_weight > 0 else 0.0
            final_probs.append(p)
            check_sum += p

        state_idx, state_name, action, qc, counts = ctrl.run_logic(*s_active)

        # Tampilan Output
        print(f"\n[SCENARIO ID: {target_scenario}]")
        print("-" * 85)
        print(f"| {'SENSOR':<10} | {'VALUE':<10} | {'UNIT':<5} | {'STATUS':<10} | {'PROB':<8} | {'TARGET':<10} |")
        print("-" * 85)
        
        sensors = [
            ("S1 NPK", v1, "V", "SS1_NUTRI", final_probs[0]),
            ("S2 Water", v2, "ADC", "SS2_WATER", final_probs[1]),
            ("S3 Temp", v3, "°C", "SS3_TEMP", final_probs[2]),
            ("S4 NDVI", v4, "NDVI", "SS4_SPRAY", final_probs[3]),
            ("S5 EC", v5, "dS/m", "SS5_FLUSH", final_probs[4]),
            ("S6 Gas", v6, "ppm", "SS6_GAS", final_probs[5]),
        ]

        for i, (name, val, unit, target, prob) in enumerate(sensors):
            status_str = "1 (ON)" if s_active[i] else "0 (OFF)"
            marker = " <<< ACTIVE" if s_active[i] else ""
            print(f"| {name:<10} | {val:<10.2f} | {unit:<5} | {status_str:<10} | {prob:<8.4f} | {target:<10} |{marker}")

        print("-" * 85)
        print(f"TOTAL PROBABILITY SUM : {check_sum:.4f}")
        
        # Visualisasi Quantum Simulation
        print("\n" + "="*30 + " QUANTUM SIMULATION RESULT " + "="*30)
        
        print(">> Circuit Topography (Transpiled):")
        print(qc.draw(output='text'))
        
        draw_text_histogram(counts)
        
        output_bin = max(counts, key=counts.get)
        print(f"\n>> Decoded Output (Q8..Q6)     : |{output_bin}⟩ (Binary State)")
        print(f"   Decimal State Index         : {state_idx}")
        
        print("\n>> CONTROLLER DECISION:")
        print(f"   CURRENT STATE : {state_name}")
        print(f"   ACTION        : {action}")
        print("="*85)

        user_input = input("\nTekan [ENTER] untuk Next Scenario, atau ketik 'q' untuk keluar: ")
        if user_input.lower() == 'q':
            print("Shutting down system...")
            break

if __name__ == "__main__":
    main()