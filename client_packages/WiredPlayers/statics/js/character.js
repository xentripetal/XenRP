let currentFatherFace = 0;
let currentMotherFace = 0;
let currentFatherSkin = 0;
let currentMotherSkin = 0;
let currentHairModel = 0;
let currentHairFirstColor = 0;
let currentHairSecondColor = 0;
let currentBeardModel = -1;
let currentBeardColor = 0;
let currentChestModel = -1;
let currentChestColor = 0;
let currentBlemishesModel = -1;
let currentAgeingModel = -1;
let currentComplexionModel = -1;
let currentSundamageModel = -1;
let currentFrecklesModel = -1;
let currentEyesColor = 0;
let currentEyebrowsModel = 0;
let currentEyebrowsColor = 0;
let currentMakeupModel = -1;
let currentMakeupColor = 0;
let currentBlushModel = -1;
let currentBlushColor = 0;
let currentLipstickModel = -1;
let currentLipstickColor = 0;

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

function hideError() {
	$('#error').addClass('no-display');
}

function toggleMenu() {
	$('#slider').slideToggle('slow');
}

function toggleCharacterInfo() {
	if($("#character-customize").is(":hidden") == false) {
		$("#character-customize").fadeOut('slow');
	}
	$('#basic-info').slideToggle('slow');
}

function toggleCharacterCustomize() {
	if($("#basic-info").is(":hidden") == false) {
		$("#basic-info").fadeOut('slow');
	}
	$('#character-customize').slideToggle('slow');
}

$("div#sex").on("click", "img", function() {
	if($(this).hasClass("enabled") == false) {
		var sex = 0;
		if($("#sex-male").hasClass("enabled") == true) {
			$("#sex-male").attr("src", "../img/character/male-disabled.png");
			$("#sex-female").attr("src", "../img/character/female-enabled.png");
			$("#sex-male").removeClass("enabled");
			$("#sex-female").addClass("enabled");
			sex = 1;
		} else {
			$("#sex-male").attr("src", "../img/character/male-enabled.png");
			$("#sex-female").attr("src", "../img/character/female-disabled.png");
			$("#sex-female").removeClass("enabled");
			$("#sex-male").addClass("enabled");
		}
		mp.trigger('updatePlayerSex', sex);
	}
});

$("nav#slider ul").on("click", "li", function() {
	// check the active index
	let position = $(this).index();
	let text = $(this).text();

	// Hide the menu
	$('#slider').slideToggle('fast');

	// Check the option pressed
	$.each($("#option-panels > div"), function(index, value) {
		if(index == position) {
			$(this).removeClass("no-display");
			$("#current-option").text(text);
		} else {
			$(this).addClass("no-display");
		}
	});
});

function createCharacter() {
	if($.trim($('#character-name').val()).length == 0) {
		$('#error-message').html(i18next.t('character.name-missing'));
		$('#error').removeClass('no-display');
	} else if($.trim($('#character-surname').val()).length == 0) {
		$('#error-message').html(i18next.t('character.surname-missing'));
		$('#error').removeClass('no-display');
	} else {
		let characterAge = $('#age').val();
		let characterName = $('#character-name').val()[0].toUpperCase() + $('#character-name').val().substr(1);
		let characterSurname = $('#character-surname').val()[0].toUpperCase() + $('#character-surname').val().substr(1);
		mp.trigger('acceptCharacterCreation', characterName.trim() + " " + characterSurname.trim(), characterAge);
	}
}

function cancelCreation() {
	// Cancel character's creation
	mp.trigger('cancelCharacterCreation');
}

function showPlayerDuplicatedWarn() {
	$('#error-message').html(i18next.t('character.name-duplicated'));
	$('#error').removeClass('no-display');
}

function cameraPointTo(part) {
	// Change the camera pointing zone
	mp.trigger('cameraPointTo', part);
}

function rotateCharacter() {
    var rotation = parseFloat(document.getElementById('character-slider').value);
	mp.trigger('rotateCharacter', rotation);
}

function showPrevFatherFace() {
	if(currentFatherFace == 0) {
		currentFatherFace = 41;
	} else {
		currentFatherFace--;
	}
	$("#face-father-shape").text(i18next.t('general.type', {value: currentFatherFace + 1}));
	mp.trigger('updatePlayerCreation', 'firstHeadShape', currentFatherFace, false);	
}

function showNextFatherFace() {
	if(currentFatherFace == 41) {
		currentFatherFace = 0;
	} else {
		currentFatherFace++;
	}
	$("#face-father-shape").text(i18next.t('general.type', {value: currentFatherFace + 1}));
	mp.trigger('updatePlayerCreation', 'firstHeadShape', currentFatherFace, false);	
}

function showPrevMotherFace() {
	if(currentMotherFace == 0) {
		currentMotherFace = 41;
	} else {
		currentMotherFace--;
	}
	$("#face-mother-shape").text(i18next.t('general.type', {value: currentMotherFace + 1}));
	mp.trigger('updatePlayerCreation', 'secondHeadShape', currentMotherFace, false);	
}

function showNextMotherFace() {
	if(currentMotherFace == 41) {
		currentMotherFace = 0;
	} else {
		currentMotherFace++;
	}
	$("#face-mother-shape").text(i18next.t('general.type', {value: currentMotherFace + 1}));
	mp.trigger('updatePlayerCreation', 'secondHeadShape', currentMotherFace, false);
}

function showPrevFatherSkin() {
	if(currentFatherSkin == 0) {
		currentFatherSkin = 45;
	} else {
		currentFatherSkin--;
	}
	$("#father-skin").text(i18next.t('general.type', {value: currentFatherSkin + 1}));
	mp.trigger('updatePlayerCreation', 'firstSkinTone', currentFatherSkin, false);
}

function showNextFatherSkin() {
	if(currentFatherSkin == 45) {
		currentFatherSkin = 0;
	} else {
		currentFatherSkin++;
	}
	$("#father-skin").text(i18next.t('general.type', {value: currentFatherSkin + 1}));
	mp.trigger('updatePlayerCreation', 'firstSkinTone', currentFatherSkin, false);
}

function showPrevMotherSkin() {
	if(currentMotherSkin == 0) {
		currentMotherSkin = 45;
	} else {
		currentMotherSkin--;
	}
	$("#mother-skin").text(i18next.t('general.type', {value: currentMotherSkin + 1}));
	mp.trigger('updatePlayerCreation', 'secondSkinTone', currentMotherSkin, false);
}

function showNextMotherSkin() {
	if(currentMotherSkin == 45) {
		currentMotherSkin = 0;
	} else {
		currentMotherSkin++;
	}
	$("#mother-skin").text(i18next.t('general.type', {value: currentMotherSkin + 1}));
	mp.trigger('updatePlayerCreation', 'secondSkinTone', currentMotherSkin, false);
}

function updateFaceMix() {
	let faceMixValue = document.getElementById('headMix').value;
	mp.trigger('updatePlayerCreation', 'headMix', faceMixValue, true);
}

function updateSkinMix() {
	let skinMixValue = document.getElementById('skinMix').value;
	mp.trigger('updatePlayerCreation', 'skinMix', skinMixValue, true);
}

function showPrevHairModel() {
	if(currentHairModel == 0) {
		currentHairModel = 38;
	} else if(currentHairModel == 24) {
		currentHairModel = currentHairModel - 2;
	} else {
	    currentHairModel--;
    }
	$("#hair-model").text(i18next.t('general.type', {value: currentHairModel + 1}));
	mp.trigger('updatePlayerCreation', 'hairModel', currentHairModel, false);
}

function showNextHairModel() {
	if(currentHairModel == 38) {
		currentHairModel = 0;
	} else if (currentHairModel == 22) {
	    currentHairModel = currentHairModel + 2;
	} else {
		currentHairModel++;
	}
	$("#hair-model").text(i18next.t('general.type', {value: currentHairModel + 1}));
	mp.trigger('updatePlayerCreation', 'hairModel', currentHairModel, false);
}

function showPrevHairFirstColor() {
	if(currentHairFirstColor == 0) {
		currentHairFirstColor = 63;
	} else {
		currentHairFirstColor--;
	}
	$("#hair-first-color").text(i18next.t('general.type', {value: currentHairFirstColor + 1}));
	mp.trigger('updatePlayerCreation', 'firstHairColor', currentHairFirstColor, false);
}

function showNextHairFirstColor() {
	if(currentHairFirstColor == 63) {
		currentHairFirstColor = 0;
	} else {
		currentHairFirstColor++;
	}
	$("#hair-first-color").text(i18next.t('general.type', {value: currentHairFirstColor + 1}));
	mp.trigger('updatePlayerCreation', 'firstHairColor', currentHairFirstColor, false);
}

function showPrevHairSecondColor() {
	if(currentHairSecondColor == 0) {
		currentHairSecondColor = 63;
	} else {
		currentHairSecondColor--;
	}
	$("#hair-second-color").text(i18next.t('general.type', {value: currentHairSecondColor + 1}));
	mp.trigger('updatePlayerCreation', 'secondHairColor', currentHairSecondColor, false);
}

function showNextHairSecondColor() {
	if(currentHairSecondColor == 63) {
		currentHairSecondColor = 0;
	} else {
		currentHairSecondColor++;
	}
	$("#hair-second-color").text(i18next.t('general.type', {value: currentHairSecondColor + 1}));
	mp.trigger('updatePlayerCreation', 'secondHairColor', currentHairSecondColor, false);
}

function showPrevBeardModel() {
	if(currentBeardModel == -1) {
		currentBeardModel = 29;
	} else {
		currentBeardModel--;
	}
	if(currentBeardModel == -1) {
		$("#beard-model").text(i18next.t('character.no-beard'));
	} else {
		$("#beard-model").text(i18next.t('general.type', {value: currentBeardModel + 1}));
	}
	mp.trigger('updatePlayerCreation', 'beardModel', currentBeardModel, false);
}

function showNextBeardModel() {
	if(currentBeardModel == 29) {
		currentBeardModel = -1;
	} else {
		currentBeardModel++;
	}
	if(currentBeardModel == -1) {
		$("#beard-model").text(i18next.t('character.no-beard'));
	} else {
		$("#beard-model").text(i18next.t('general.type', {value: currentBeardModel + 1}));
	}
	mp.trigger('updatePlayerCreation', 'beardModel', currentBeardModel, false);
}

function showPrevBeardColor() {
	if(currentBeardColor == 0) {
		currentBeardColor = 63;
	} else {
		currentBeardColor--;
	}
	$("#beard-color").text(i18next.t('general.type', {value: currentBeardColor + 1}));
	mp.trigger('updatePlayerCreation', 'bearColor', currentBeardColor, false);
}

function showNextBeardColor() {
	if(currentBeardColor == 63) {
		currentBeardColor = 0;
	} else {
		currentBeardColor++;
	}
	$("#beard-color").text(i18next.t('general.type', {value: currentBeardColor + 1}));
	mp.trigger('updatePlayerCreation', 'bearColor', currentBeardColor, false);
}

function showPrevChestModel() {
	if(currentChestModel == -1) {
		currentChestModel = 17;
	} else {
		currentChestModel--;
	}
	if(currentChestModel == -1) {
		$("#chest-model").text(i18next.t('character.no-hair'));
	} else {
		$("#chest-model").text(i18next.t('general.type', {value: currentChestModel + 1}));
	}
	mp.trigger('updatePlayerCreation', 'chestModel', currentChestModel, false);
}

function showNextChestModel() {
	if(currentChestModel == 17) {
		currentChestModel = -1;
	} else {
		currentChestModel++;
	}
	if(currentChestModel == -1) {
		$("#chest-model").text(i18next.t('character.no-hair'));
	} else {
		$("#chest-model").text(i18next.t('general.type', {value: currentChestModel + 1}));
	}
	mp.trigger('updatePlayerCreation', 'chestModel', currentChestModel, false);
}

function showPrevChestColor() {
	if(currentChestColor == 0) {
		currentChestColor = 63;
	} else {
		currentChestColor--;
	}
	$("#chest-color").text(i18next.t('general.type', {value: currentChestColor + 1}));
	mp.trigger('updatePlayerCreation', 'chestColor', currentChestColor, false);
}

function showNextChestColor() {
	if(currentChestColor == 63) {
		currentChestColor = 0;
	} else {
		currentChestColor++;
	}
	$("#chest-color").text(i18next.t('general.type', {value: currentChestColor + 1}));
	mp.trigger('updatePlayerCreation', 'chestColor', currentChestColor, false);
}

function showPrevBlemishesModel() {
	if(currentBlemishesModel == -1) {
		currentBlemishesModel = 24;
	} else {
		currentBlemishesModel--;
	}
	if(currentBlemishesModel == -1) {
		$("#blemishes-model").text(i18next.t('character.no-blemishes'));
	} else {
		$("#blemishes-model").text(i18next.t('general.type', {value: currentBlemishesModel + 1}));
	}
	mp.trigger('updatePlayerCreation', 'blemishesModel', currentBlemishesModel, false);
}

function showNextBlemishesModel() {
	if(currentBlemishesModel == 24) {
		currentBlemishesModel = -1;
	} else {
		currentBlemishesModel++;
	}
	if(currentBlemishesModel == -1) {
		$("#blemishes-model").text(i18next.t('character.no-blemishes'));
	} else {
		$("#blemishes-model").text(i18next.t('general.type', {value: currentBlemishesModel + 1}));
	}
	mp.trigger('updatePlayerCreation', 'blemishesModel', currentBlemishesModel, false);
}

function showPrevAgeingModel() {
	if(currentAgeingModel == -1) {
		currentAgeingModel = 15;
	} else {
		currentAgeingModel--;
	}
	if(currentAgeingModel == -1) {
		$("#ageing-model").text(i18next.t('character.no-ageing'));
	} else {
		$("#ageing-model").text(i18next.t('general.type', {value: currentAgeingModel + 1}));
	}
	mp.trigger('updatePlayerCreation', 'ageingModel', currentAgeingModel, false);
}

function showNextAgeingModel() {
	if(currentAgeingModel == 15) {
		currentAgeingModel = -1;
	} else {
		currentAgeingModel++;
	}
	if(currentAgeingModel == -1) {
		$("#ageing-model").text(i18next.t('character.no-ageing'));
	} else {
		$("#ageing-model").text(i18next.t('general.type', {value: currentAgeingModel + 1}));
	}
	mp.trigger('updatePlayerCreation', 'ageingModel', currentAgeingModel, false);
}

function showPrevComplexionModel() {
	if(currentComplexionModel == -1) {
		currentComplexionModel = 12;
	} else {
		currentComplexionModel--;
	}
	if(currentComplexionModel == -1) {
		$("#complexion-model").text(i18next.t('character.no-complexion'));
	} else {
		$("#complexion-model").text(i18next.t('general.type', {value: currentComplexionModel + 1}));
	}
	mp.trigger('updatePlayerCreation', 'complexionModel', currentComplexionModel, false);
}

function showNextComplexionModel() {
	if(currentComplexionModel == 12) {
		currentComplexionModel = -1;
	} else {
		currentComplexionModel++;
	}
	if(currentComplexionModel == -1) {
		$("#complexion-model").text(i18next.t('character.no-complexion'));
	} else {
		$("#complexion-model").text(i18next.t('general.type', {value: currentComplexionModel + 1}));
	}
	mp.trigger('updatePlayerCreation', 'complexionModel', currentComplexionModel, false);
}

function showPrevSundamageModel() {
	if(currentSundamageModel == -1) {
		currentSundamageModel = 11;
	} else {
		currentSundamageModel--;
	}
	if(currentSundamageModel == -1) {
		$("#sundamage-model").text(i18next.t('character.no-sundamage'));
	} else {
		$("#sundamage-model").text(i18next.t('general.type', {value: currentSundamageModel + 1}));
	}
	mp.trigger('updatePlayerCreation', 'sundamageModel', currentSundamageModel, false);
}

function showNextSundamageModel() {
	if(currentSundamageModel == 11) {
		currentSundamageModel = -1;
	} else {
		currentSundamageModel++;
	}
	if(currentSundamageModel == -1) {
		$("#sundamage-model").text(i18next.t('character.no-sundamage'));
	} else {
		$("#sundamage-model").text(i18next.t('general.type', {value: currentSundamageModel + 1}));
	}
	mp.trigger('updatePlayerCreation', 'sundamageModel', currentSundamageModel, false);
}

function showPrevFrecklesModel() {
	if(currentFrecklesModel == -1) {
		currentFrecklesModel = 18;
	} else {
		currentFrecklesModel--;
	}
	if(currentFrecklesModel == -1) {
		$("#freckles-model").text(i18next.t('character.no-freckles'));
	} else {
		$("#freckles-model").text(i18next.t('general.type', {value: currentFrecklesModel + 1}));
	}
	mp.trigger('updatePlayerCreation', 'frecklesModel', currentFrecklesModel, false);
}

function showNextFrecklesModel() {
	if(currentFrecklesModel == 18) {
		currentFrecklesModel = -1;
	} else {
		currentFrecklesModel++;
	}
	if(currentFrecklesModel == -1) {
		$("#freckles-model").text(i18next.t('character.no-freckles'));
	} else {
		$("#freckles-model").text(i18next.t('general.type', {value: currentFrecklesModel + 1}));
	}
	mp.trigger('updatePlayerCreation', 'frecklesModel', currentFrecklesModel, false);
}

function showPrevEyesColor() {
	if(currentEyesColor == 0) {
		currentEyesColor = 31;
	} else {
		currentEyesColor--;
	}
	$("#eyes-color").text(i18next.t('general.type', {value: currentEyesColor + 1}));
	mp.trigger('updatePlayerCreation', 'eyesColor', currentEyesColor, false);
}

function showNextEyesColor() {
	if(currentEyesColor == 31) {
		currentEyesColor = 0;
	} else {
		currentEyesColor++;
	}
	$("#eyes-color").text(i18next.t('general.type', {value: currentEyesColor + 1}));
	mp.trigger('updatePlayerCreation', 'eyesColor', currentEyesColor, false);
}

function showPrevEyebrowsModel() {
	if(currentEyebrowsModel == 0) {
		currentEyebrowsModel = 33;
	} else {
		currentEyebrowsModel--;
	}
	$("#eyebrows-model").text(i18next.t('general.type', {value: currentEyebrowsModel + 1}));
	mp.trigger('updatePlayerCreation', 'eyebrowsModel', currentEyebrowsModel, false);
}

function showNextEyebrowsModel() {
	if(currentEyebrowsModel == 33) {
		currentEyebrowsModel = 0;
	} else {
		currentEyebrowsModel++;
	}
	$("#eyebrows-model").text(i18next.t('general.type', {value: currentEyebrowsModel + 1}));
	mp.trigger('updatePlayerCreation', 'eyebrowsModel', currentEyebrowsModel, false);
}

function showPrevEyebrowsColor() {
	if(currentEyebrowsColor == 0) {
		currentEyebrowsColor = 63;
	} else {
		currentEyebrowsColor--;
	}
	$("#eyebrows-color").text(i18next.t('general.type', {value: currentEyebrowsColor + 1}));
	mp.trigger('updatePlayerCreation', 'eyebrowsColor', currentEyebrowsColor, false);
}

function showNextEyebrowsColor() {
	if(currentEyebrowsColor == 63) {
		currentEyebrowsColor = 0;
	} else {
		currentEyebrowsColor++;
	}
	$("#eyebrows-color").text(i18next.t('general.type', {value: currentEyebrowsColor + 1}));
	mp.trigger('updatePlayerCreation', 'eyebrowsColor', currentEyebrowsColor, false);
}

function updateFaceFeature(feature) {
	let faceValue = document.getElementById(feature).value;
	mp.trigger('updatePlayerCreation', feature, faceValue, true);
}

function showPrevMakeupModel() {
	if(currentMakeupModel == -1) {
		currentMakeupModel = 75;
	} else {
		currentMakeupModel--;
	}
	if(currentMakeupModel == -1) {
		$("#makeup-model").text(i18next.t('character.no-makeup'));
	} else {
		$("#makeup-model").text(i18next.t('general.type', {value: currentMakeupModel + 1}));
	}
	mp.trigger('updatePlayerCreation', 'makeupModel', currentMakeupModel, false);
}

function showNextMakeupModel() {
	if(currentMakeupModel == 75) {
		currentMakeupModel = -1;
	} else {
		currentMakeupModel++;
	}
	if(currentMakeupModel == -1) {
		$("#makeup-model").text(i18next.t('character.no-makeup'));
	} else {
		$("#makeup-model").text(i18next.t('general.type', {value: currentMakeupModel + 1}));
	}
	mp.trigger('updatePlayerCreation', 'makeupModel', currentMakeupModel, false);
}

function showPrevBlushModel() {
	if(currentBlushModel == -1) {
		currentBlushModel = 7;
	} else {
		currentBlushModel--;
	}
	if(currentBlushModel == -1) {
		$("#blush-model").text(i18next.t('character.no-blush'));
	} else {
		$("#blush-model").text(i18next.t('general.type', {value: currentBlushModel + 1}));
	}
	mp.trigger('updatePlayerCreation', 'blushModel', currentBlushModel, false);
}

function showNextBlushModel() {
	if(currentBlushModel == 7) {
		currentBlushModel = -1;
	} else {
		currentBlushModel++;
	}
	if(currentBlushModel == -1) {
		$("#blush-model").text(i18next.t('character.no-blush'));
	} else {
		$("#blush-model").text(i18next.t('general.type', {value: currentBlushModel + 1}));
	}
	mp.trigger('updatePlayerCreation', 'blushModel', currentBlushModel, false);
}

function showPrevBlushColor() {
	if(currentBlushColor == 0) {
		currentBlushColor = 63;
	} else {
		currentBlushColor--;
	}
	$("#blush-color").text(i18next.t('general.type', {value: currentBlushColor + 1}));
	mp.trigger('updatePlayerCreation', 'blushColor', currentBlushColor, false);
}

function showNextBlushColor() {
	if(currentBlushColor == 63) {
		currentBlushColor = 0;
	} else {
		currentBlushColor++;
	}
	$("#blush-color").text(i18next.t('general.type', {value: currentBlushColor + 1}));
	mp.trigger('updatePlayerCreation', 'blushColor', currentBlushColor, false);
}

function showPrevLipstickModel() {
	if(currentLipstickModel == -1) {
		currentLipstickModel = 10;
	} else {
		currentLipstickModel--;
	}
	if(currentLipstickModel == -1) {
		$("#lipstick-model").text(i18next.t('character.no-lipstick'));
	} else {
		$("#lipstick-model").text(i18next.t('general.type', {value: currentLipstickModel + 1}));
	}
	mp.trigger('updatePlayerCreation', 'lipstickModel', currentLipstickModel, false);
}

function showNextLipstickModel() {
	if(currentLipstickModel == 10) {
		currentLipstickModel = -1;
	} else {
		currentLipstickModel++;
	}
	if(currentLipstickModel == -1) {
		$("#lipstick-model").text(i18next.t('character.no-lipstick'));
	} else {
		$("#lipstick-model").text(i18next.t('general.type', {value: currentLipstickModel + 1}));
	}
	mp.trigger('updatePlayerCreation', 'lipstickModel', currentLipstickModel, false);
}

function showPrevLipstickColor() {
	if(currentLipstickColor == 0) {
		currentLipstickColor = 63;
	} else {
		currentLipstickColor--;
	}
	$("#lipstick-color").text(i18next.t('general.type', {value: currentLipstickColor + 1}));
	mp.trigger('updatePlayerCreation', 'lipstickColor', currentLipstickColor, false);
}

function showNextLipstickColor() {
	if(currentLipstickColor == 63) {
		currentLipstickColor = 0;
	} else {
		currentLipstickColor++;
	}
	$("#lipstick-color").text(i18next.t('general.type', {value: currentLipstickColor + 1}));
	mp.trigger('updatePlayerCreation', 'lipstickColor', currentLipstickColor, false);
}

$('.btn-number').click(function(e){
    e.preventDefault();
    
    fieldName = $(this).attr('data-field');
    type      = $(this).attr('data-type');
    var input = $("input[name='"+fieldName+"']");
    var currentVal = parseInt(input.val());
    if (!isNaN(currentVal)) {
        if(type == 'minus') {
            
            if(currentVal > input.attr('min')) {
                input.val(currentVal - 1).change();
            } 
            if(parseInt(input.val()) == input.attr('min')) {
                $(this).attr('disabled', true);
            }

        } else if(type == 'plus') {

            if(currentVal < input.attr('max')) {
                input.val(currentVal + 1).change();
            }
            if(parseInt(input.val()) == input.attr('max')) {
                $(this).attr('disabled', true);
            }

        }
    } else {
        input.val(0);
    }
});
$('.input-number').focusin(function(){
   $(this).data('oldValue', $(this).val());
});
$('.input-number').change(function() {
    
    minValue =  parseInt($(this).attr('min'));
    maxValue =  parseInt($(this).attr('max'));
    valueCurrent = parseInt($(this).val());
    
    name = $(this).attr('name');
    if(valueCurrent >= minValue) {
        $(".btn-number[data-type='minus'][data-field='"+name+"']").removeAttr('disabled')
    } else {
        $(this).val($(this).data('oldValue'));
    }
    if(valueCurrent <= maxValue) {
        $(".btn-number[data-type='plus'][data-field='"+name+"']").removeAttr('disabled')
    } else {
        $(this).val($(this).data('oldValue'));
    }
    
    
});
$(".input-number").keydown(function (e) {
        // Allow: backspace, delete, tab, escape, enter and .
        if ($.inArray(e.keyCode, [46, 8, 9, 27, 13, 190]) !== -1 ||
             // Allow: Ctrl+A
            (e.keyCode == 65 && e.ctrlKey === true) || 
             // Allow: home, end, left, right
            (e.keyCode >= 35 && e.keyCode <= 39)) {
                 // let it happen, don't do anything
                 return;
        }
        // Ensure that it is a number and stop the keypress
        if ((e.shiftKey || (e.keyCode < 48 || e.keyCode > 57)) && (e.keyCode < 96 || e.keyCode > 105)) {
            e.preventDefault();
        }
	});
	

/* Composure/Psycho */

$(".btn-age").on("click", function() {
	var $button = $(this);

	if ($button.attr('data-dir') == 'minus') {
		if ($('#age').val() > 12) {
			$('#age').val(parseInt($('#age').val()) - 1);	
			if($('#age').val() < 90) {
				$('#btn-age-plus').attr('disabled', false);
			}
		} else {
			$button.attr('disabled', true);
		}
	} else if ($button.attr('data-dir') == 'plus') {
		if ($('#age').val() < 90) {
			$('#age').val(parseInt($('#age').val()) + 1);	
			if($('#age').val() > 12) {
				$('#btn-age-minus').attr('disabled', false);
			}
		} else {
			$button.attr('disabled', true);
		}
	}
});