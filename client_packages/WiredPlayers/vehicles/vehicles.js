let vehicleLocationBlip = undefined; 

mp.events.add('locateVehicle', (position) => {
	// Create the blip on the map
	vehicleLocationBlip = mp.blips.new(1, position, {color: 1});
});

mp.events.add('deleteVehicleLocation', () => {
	// Destroy the blip on the map
	vehicleLocationBlip.destroy();
	vehicleLocationBlip = undefined;
});