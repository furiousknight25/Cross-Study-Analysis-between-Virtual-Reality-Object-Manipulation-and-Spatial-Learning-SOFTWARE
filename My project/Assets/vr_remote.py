import socket
import tkinter as tk

# ==========================================
# CONFIGURATION - UPDATE THIS IN THE FIELD
# ==========================================
# Change this to the IP address you found in the Quest 3 Wi-Fi settings
QUEST_IP = "10.119.177.120"
# QUEST_IP = "192.168.1.41"
UDP_PORT = 5005
MESSAGE = b"TRIGGER_TRIAL"

def send_signal():
    try:
        # Create a UDP socket and fire the packet
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        sock.sendto(MESSAGE, (QUEST_IP, UDP_PORT))
        print(f"[SUCCESS] Signal fired to {QUEST_IP}:{UDP_PORT}")
        
        # UI Feedback
        status_label.config(text=f"Signal Sent!", fg="#00ff00")
        root.after(2000, lambda: status_label.config(text="Standing By", fg="white"))
        
    except Exception as e:
        print(f"[ERROR] Failed to send signal: {e}")
        status_label.config(text="Network Error!", fg="red")

# ==========================================
# GUI CONSTRUCTION
# ==========================================
root = tk.Tk()
root.title("UMD VR Thesis Control")
root.geometry("400x250")
root.configure(bg="#2b2b2b")

# Title Label
title = tk.Label(root, text="Experiment FSM Controller", font=("Arial", 16, "bold"), bg="#2b2b2b", fg="white")
title.pack(pady=15)

# The Big Trigger Button
trigger_btn = tk.Button(
    root, 
    text="ADVANCE STATE\n(Next Phase)", 
    command=send_signal,
    bg="#007acc",
    fg="white",
    font=("Arial", 18, "bold"),
    activebackground="#005999",
    activeforeground="white",
    cursor="hand2"
)
trigger_btn.pack(expand=True, fill="both", padx=30, pady=10)

# Status Label
status_label = tk.Label(root, text="Standing By", font=("Arial", 12, "italic"), bg="#2b2b2b", fg="white")
status_label.pack(pady=10)

root.mainloop()
