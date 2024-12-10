import socket

# Set up the server address and port to connect to
TCP_IP = "192.168.137.41"  # Replace with the actual IP address of the Unity machine
TCP_PORT = 8888
BUFFER_SIZE = 4096  # Buffer size to receive data

# Create a socket object for TCP connection
sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

# Connect to the Unity TCP server
sock.connect((TCP_IP, TCP_PORT))
print(f"Connected to {TCP_IP}:{TCP_PORT}")

# Create a socket object for TCP connection
sock2 = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

# Connect to the Unity TCP server
sock2.connect((TCP_IP, 8889))
print(f"Connected to {TCP_IP}:{8889}")

# Create a socket object for TCP connection
sock3 = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

# Connect to the Unity TCP server
sock3.connect((TCP_IP, 8890))
print(f"Connected to {TCP_IP}:{8890}")

try:
    while True:
        # Receive the data from the server
        data = sock.recv(BUFFER_SIZE)
        if data:
            print("Received:", data.decode())

        # left hand tracking
        data = sock2.recv(BUFFER_SIZE)
        if data:
            print("Received:", data.decode())

        # right hand tracking
        data = sock3.recv(BUFFER_SIZE)
        if data:
            print("Received:", data.decode())

finally:
    # Close the connection when done (though this will never be reached in an infinite loop)
    sock.close()
