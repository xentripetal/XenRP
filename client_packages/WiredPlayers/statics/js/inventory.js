let selected = undefined;

$(document).ready(function() {
	i18next.use(window.i18nextXHRBackend).init({
		backend: {
			loadPath: '../i18n/en.json'
		}
	}, function(err, t) {
		jqueryI18next.init(i18next, $);
		$(document).localize();
	});
});

function populateInventory(inventoryJson, title) {
	// Initialize the selection
	selected = undefined;
	
	// Get the items in the inventory
	let inventory = JSON.parse(inventoryJson);
	
	// Get the item containers
	let titleContainer = document.getElementById('identifier');
	let inventoryContainer = document.getElementById('inventory');
	
	for(let i = 0; i < inventory.length; i++) {
		// Get each item
		let item = inventory[i];
		
		// Create the elements to show the items
		let itemContainer = document.createElement('div');
		let amountContainer = document.createElement('div');
		let itemImage = document.createElement('img');
		
		// Get the needed classes
		itemContainer.classList.add('inventory-item');
		amountContainer.classList.add('inventory-amount');
		
		// Get the content of each item
		itemImage.src = '../img/inventory/' + item.hash + '.png';
		amountContainer.textContent = item.amount;
		
		itemContainer.onclick = (function() {
			// Check if a new item has been selected
			if(selected !== i) {
				// Get the previous selection
				if(selected != undefined) {
					let previousSelected = document.getElementsByClassName('inventory-item')[selected];
					previousSelected.classList.remove('active-item');
				}
				
				// Select the clicked element
				let currentSelected = document.getElementsByClassName('inventory-item')[i];
				currentSelected.classList.add('active-item');
				selected = i;
				
				// Show the options
				mp.trigger('getInventoryOptions', item.type, item.hash);
			}
		});
		
		// Create the item hierarchy	
		inventoryContainer.appendChild(itemContainer);
		itemContainer.appendChild(amountContainer);
		itemContainer.appendChild(itemImage);		
	}
}

function showInventoryOptions(optionsArray, dropable) {
	// Add the options
	for(let i = 0; i < optionsArray; i++) {

	}
	
	if(dropable) {
		// Add drop option

	}
}