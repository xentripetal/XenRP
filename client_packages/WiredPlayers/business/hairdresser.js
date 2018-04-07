const maleFaceOptions = [
	{'desc': 'hairdresser.hair', 'minValue': 0, 'maxValue': 36}, {'desc': 'hairdresser.hair-primary', 'minValue': 0, 'maxValue': 63}, 
	{'desc': 'hairdresser.hair-secondary', 'minValue': 0, 'maxValue': 63}, {'desc': 'hairdresser.eyebrows', 'minValue': 0, 'maxValue': 33}, 
	{'desc': 'hairdresser.eyebrows-color', 'minValue': 0, 'maxValue': 63}, {'desc': 'hairdresser.beard', 'minValue': -1, 'maxValue': 36}, 
	{'desc': 'hairdresser.beard-color', 'minValue': 0, 'maxValue': 63}
];

const femaleFaceOptions = [
	{'desc': 'hairdresser.hair', 'minValue': 0, 'maxValue': 38}, {'desc': 'hairdresser.hair-primary', 'minValue': 0, 'maxValue': 63}, 
	{'desc': 'hairdresser.hair-secondary', 'minValue': 0, 'maxValue': 63}, {'desc': 'hairdresser.eyebrows', 'minValue': 0, 'maxValue': 33}, 
	{'desc': 'hairdresser.eyebrows-color', 'minValue': 0, 'maxValue': 63}
];

let faceHairArray = [];

mp.events.add('showHairdresserMenu', (businessName) => {
	// Get the player and its sex
	let player = mp.players.local;
	let sex = player.getVariable('PLAYER_SEX');
	
	// Add the options
	let faceOptions = JSON.stringify(sex === 0 ? maleFaceOptions : femaleFaceOptions);
	
	// Inicializamos los valores
	faceHairArray.push(player.getVariable('HAIR_MODEL'));
	faceHairArray.push(player.getVariable('FIRST_HAIR_COLOR'));
	faceHairArray.push(player.getVariable('SECOND_HAIR_COLOR'));
	faceHairArray.push(player.getVariable('EYEBROWS_MODEL'));
	faceHairArray.push(player.getVariable('EYEBROWS_COLOR'));
	faceHairArray.push(player.getVariable('BEARD_MODEL'));
	faceHairArray.push(player.getVariable('BEARD_COLOR'));
	
	// Create hairdressers' menu
	mp.events.call('createBrowser', ['package://WiredPlayers/statics/html/sideMenu.html', 'populateHairdresserMenu', faceOptions, JSON.stringify(faceHairArray), businessName]);
});

mp.events.add('updateFacialHair', (slot, value) => {
	// Get the player
	let player = mp.players.local;
	
	// Save the new value
	faceHairArray[slot] = value;
	
	// Update the player's head
	player.setComponentVariation(2, faceHairArray[0], 0, 0);
	player.setHairColor(faceHairArray[1], faceHairArray[2]);
	player.setHeadOverlay(1, faceHairArray[5], 0.99, faceHairArray[6], 0);
	player.setHeadOverlay(2, faceHairArray[3], 0.99, faceHairArray[4], 0);
});

mp.events.add('applyHairdresserChanges', () => {
	let generatedFace = {};
	
	generatedFace.hairModel = faceHairArray[0];
	generatedFace.firstHairColor = faceHairArray[1];
	generatedFace.secondHairColor = faceHairArray[2];
	generatedFace.eyebrowsModel = faceHairArray[3];
	generatedFace.eyebrowsColor = faceHairArray[4];
	generatedFace.beardModel = faceHairArray[5];
	generatedFace.beardColor = faceHairArray[6];

	// Apply the changes to the player
	mp.events.callRemote('changeHairStyle', JSON.stringify(generatedFace));
});

mp.events.add('cancelHairdresserChanges', () => {
	// Get the player
	let player = mp.players.local;

	// Load the variables
	let hairModel = player.getVariable('HAIR_MODEL');
	let firstHairColor = player.getVariable('FIRST_HAIR_COLOR');
	let secondHairColor = player.getVariable('SECOND_HAIR_COLOR');
	let eyebrowsModel = player.getVariable('EYEBROWS_MODEL');
	let eyebrowsColor = player.getVariable('EYEBROWS_COLOR');
	let beardModel = player.getVariable('BEARD_MODEL');
	let beardColor = player.getVariable('BEARD_COLOR');

	// Revert the changes
	player.setComponentVariation(2, hairModel, 0, 0);
	player.setHairColor(firstHairColor, secondHairColor);
	player.setHeadOverlay(1, beardModel, 0.99, beardColor, 0);
	player.setHeadOverlay(2, eyebrowsModel, 0.99, eyebrowsColor, 0);
});