let movingFurniture = false;
let furnitureList = undefined;
let selectedFurniture = undefined;

mp.events.add('moveFurniture', (furnitureListJson) => {
    // Get the furniture available to move
    furnitureList = JSON.parse(furnitureListJson);

    // Enable the cursor and disable the chat
    mp.gui.cursor.show(true, true);
    //mp.gui.chat.show(false);

    // Set the flag for moving furniture
    movingFurniture = true;
});

mp.events.add('click', (x, y, upOrDown, leftOrRight, relativeX, relativeY, worldPosition, hitEntity) => {
    // Check if the player can move the furniture
    if(!movingFurniture || leftOrRight === "right") return;

    // Check if the player clicked on any furniture
    if(selectedFurniture === undefined && upOrDown === "down") {
        selectedFurniture = getClickedFurniture(relativeX, relativeY);
        mp.gui.chat.push("Furniture: " + selectedFurniture);
        return;
    }

    // Check if the player stopped holding the furniture
    if(selectedFurniture !== undefined && upOrDown === "up") {
        mp.events.callRemote("updateFurniturePosition");
        selectedFurniture = undefined;
        return;
    }
});

function getClickedFurniture(posX, posY) {
    let screenPosition = new mp.Vector3(posX, posY, 0);
    let worldPosition = mp.game.graphics.screen2dToWorld3d(screenPosition);

    for(let i = 0; i < furnitureList.length; i++) {
        // Obtain the furniture's position
        let furniturePosition = mp.objects.atRemoteId(furnitureList[i].handle.Value).getCoords(true);
        
        mp.gui.chat.push("D: " + distanceTo(furniturePosition, worldPosition));

        if(distanceTo(furniturePosition, worldPosition) < 15.0) {
            // Select the furniture as movable
            mp.gui.chat.push("" + i);
            return i;
        }
    }

    return undefined;
}

function distanceTo(fromVector, toVector) {
    // Get the difference between each Vector's points
    let diffX = Math.pow((fromVector.x - toVector.x), 2);
    let diffY = Math.pow((fromVector.y - toVector.y), 2);
    let diffZ = Math.pow((fromVector.z - toVector.z), 2);

    return Math.sqrt(diffX + diffY + diffZ);
}