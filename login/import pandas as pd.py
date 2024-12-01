import pandas as pd
import bluetooth
import asyncio

# Load the Excel file with registered users
file_path = r"c:\Users\Lenovo\Desktop\login\bloutooth.xlsx"  # Replace with your Excel file path
users_df = pd.read_excel(file_path)

# Extract registered MAC addresses for quick lookup
registered_macs = set(users_df["mac_address"])

# Function to scan Bluetooth devices and check for login
async def continuous_bluetooth_login(scan_duration=5, cycles=5):
    print(f"Starting continuous scanning for Bluetooth devices (each scan lasts {scan_duration} seconds)...")

    try:
        while True:  # Continuous scanning loop
            print("Scanning for Bluetooth devices...")
            all_devices = []  # List to collect all devices detected in the current cycle

            # Perform multiple scan cycles per round
            for _ in range(cycles):
                # Start a Bluetooth scan for Classic Bluetooth devices (both BLE and Classic)
                nearby_devices = bluetooth.discover_devices(duration=scan_duration, lookup_names=True, device_id=-1, flush_cache=True)

                for addr, name in nearby_devices:
                    all_devices.append((addr, name))

            # Remove duplicates based on MAC address
            unique_devices = {device[0]: device[1] for device in all_devices}.items()

            if not unique_devices:
                print("No devices found during this scan.")
            else:
                # Loop through detected devices
                for mac_address, device_name in unique_devices:
                    print(f"Detected device: {device_name}, MAC Address: {mac_address}")

                    # Check if the MAC address matches a registered user
                    if mac_address in registered_macs:
                        # Fetch user details from the Excel file
                        user_data = users_df.loc[users_df["mac_address"] == mac_address]
                        user = user_data.iloc[0]  # Get the matched user profile
                        
                        print(f"Login successful for user: {user['Name']}")
                        print(f"Role: {user['role']}")
                        print(f"History: {user['history']}")
                        print(f"Profile Image Path: {user['image']}\n")
                    else:
                        print(f"Unregistered device detected: {mac_address} (Name: {device_name}).\n")

            print("Scan complete. Restarting scan in 5 seconds...\n")
            await asyncio.sleep(5)  # Pause before the next scan cycle
    except Exception as e:
        print(f"An error occurred during Bluetooth scanning: {e}")

def main():
    try:
        asyncio.run(continuous_bluetooth_login(scan_duration=10, cycles=5))
    except RuntimeError as e:
        if "This event loop is already running" in str(e):
            print("Event loop is already running. Executing coroutine with ensure_future().")
            task = asyncio.ensure_future(continuous_bluetooth_login(scan_duration=5))
            asyncio.get_event_loop().run_until_complete(task)
        else:
            raise

if __name__ == "__main__":
    try:
        # If there's already a running loop, use an alternative
        loop = asyncio.get_running_loop()
        print("Running in an interactive environment. Using ensure_future().")
        task = asyncio.ensure_future(continuous_bluetooth_login(scan_duration=5))
    except RuntimeError:
        print("Running in a standard environment. Using asyncio.run().")
        asyncio.run(continuous_bluetooth_login(scan_duration=10))
