namespace XenRP.messages.error {
    public static class ErrRes {
        public const string player_owns_clothes = "You already own that.";
        public const string not_in_work_place = "You are not in your work place.";
        public const string vehicle_tank_full = "The vehicle's tank is full.";
        public const string vehicle_refuel_into_vehicle = "You must leave the vehicle to refuel";
        public const string not_rented_house = "You have not rented a house.";
        public const string not_in_house_door = "You are not at the door of your house.";
        public const string not_emergency_vehicle = "You are not in an emergency vehicle.";
        public const string license_exam_failed = "You failed the theoretical exam.";
        public const string license_drive_failed = "You failed the practical exam.";
        public const string player_has_identification = "You already have you identification card.";
        public const string player_not_identification_money = "You need {0}$ to get your identification card.";
        public const string player_has_medical_insurance = "You already have a medical insurance.";
        public const string player_not_medical_insurance = "You don't have a medical insurance.";
        public const string player_not_medical_insurance_money = "You need {0}$ to get a medical insurance.";
        public const string player_has_taxi_license = "You already have a taxi license.";
        public const string player_not_taxi_license_money = "You need {0}$ to get the taxi license.";
        public const string player_no_fines = "You didn't select any fine to pay.";
        public const string player_not_fine_money = "You need {0}$ to pay your fines.";
        public const string weapon_license_expired = "Your weapon license has expired.";
        public const string player_cant_chat = "You can't use the chat in the lobby.";
        public const string player_cant_command = "You can't use any command in the lobby.";
        public const string no_vehicles_near = "You don't have any vehicle near you.";
        public const string not_in_vehicle = "You aren't inside any vehicle.";
        public const string not_vehicle_driving = "You're not driving the vehicle.";
        public const string client_not_vehicle_driving = "The client must be on driver's seat.";
        public const string not_vehicle_passenger = "You aren't on passenger's seat.";
        public const string carshop_spawn_occupied = "All the parking places of the car dealer are occupied.";
        public const string not_in_carshop = "You're not in any cat dealer.";
        public const string not_in_car_practice = "You're not doing the car practical exam.";
        public const string license_failed_not_in_vehicle = "You failed the exam for not returning to your vehicle.";
        public const string car_license_achieved = "You already have a car license.";
        public const string no_character_selected = "You didn't select any character.";
        public const string not_in_vehicle_faction = "You're not in the same faction this vehicle is.";
        public const string admin_vehicle = "This vehicle can only be accessed by the staff.";
        public const string not_in_vehicle_job = "You don't have the required job to drive this vehicle.";
        public const string not_car_keys = "You don't have this vehicle's keys.";
        public const string carshop_no_money = "You need at least {0}$ in your bank account to purchase this vehicle.";
        public const string player_not_found = "The target player is not connected to the server.";
        public const string player_not_dead = "The target player is not dead.";
        public const string player_not_state_faction = "You aren't part of any state faction.";
        public const string player_job_state_faction = "You can apply for this job being part of a state faction.";
        public const string radio_frequency_none = "You're not connected to any radio frequency.";
        public const string player_not_police_faction = "You aren't part of the police department.";
        public const string player_not_news_faction = "You aren't a journalist.";
        public const string player_not_emergency_faction = "You aren't part of the emergency department.";

        public const string player_not_police_emergency_faction =
            "You aren't part of the police department neither emergency department.";

        public const string player_not_own_death = "You cannot tend to yourself.";

        public const string player_not_in_house = "You're not inside any house.";
        public const string player_not_house_owner = "You're not the owner of this house.";
        public const string player_house_rented = "You already have a house rented.";
        public const string house_not_buyable = "This house is not for sale.";
        public const string house_not_rentable = "This house is not rentable.";
        public const string house_already_rentable = "This house is already rentable.";

        public const string house_not_money =
            "You don't have the required money to buy this house in your bank account.";

        public const string player_not_rent_money = "You don't have the required money to pay the rental fee.";
        public const string house_locked = "This house is locked.";
        public const string house_not_exists = "Couldn't find any house with that identifier.";
        public const string house_occupied = "You can't sell the house as it has people living on it.";
        public const string house_sell_generic = "There's been an error selling the house, check if it's still yours.";
        public const string price_positive = "Price must be greater than 0$.";
        public const string player_job_restriction = "You haven't spent the required time in your current job.";
        public const string player_has_job = "You already have a job.";
        public const string player_no_job = "You don't have any job.";
        public const string player_no_faction = "You're not in any faction.";
        public const string not_service = "You're not in duty.";
        public const string not_fastfood = "You're not fastfood deliverer.";
        public const string order_none = "There's no order in the list to attend.";
        public const string order_taken = "This order has already been attended.";
        public const string order_timeout = "The client has tired of waiting his order.";
        public const string order_delivering = "You're already delivering an order.";
        public const string not_delivering_order = "You didn't take any order.";
        public const string deliver_in_vehicle = "Get down from the motorcycle to deliver the order.";
        public const string not_your_job_vehicle = "This is not your job vehicle.";
        public const string player_too_far = "You're too far away from the target player.";
        public const string negative_result = "{0} got a negative result.";
        public const string not_thief = "You're not a thief.";
        public const string not_hooker = "You're not a hooker.";
        public const string already_lockpicking = "You're already lockpicking.";
        public const string already_fucking = "You're in the middle of a service with your client.";
        public const string player_cant_lockpick_own_vehicle = "You can't lockpick your own vehicle.";
        public const string player_cant_hotwire_own_vehicle = "You can't hotwire your own vehicle.";
        public const string player_cant_rob_own_house = "You can't break into your own house.";
        public const string player_cant_rob_own_vehicle = "You can't steal your own vehicle.";

        public const string player_cooldown_thief =
            "You're still nervous from your last robbery, you'll have to wait 1 hour.";

        public const string veh_already_unlocked = "This vehicle is already unlocked.";
        public const string player_not_mechanic = "You're not mechanic.";
        public const string no_police_controls = "There's no police control saved.";
        public const string player_cant_rob = "There's nothing to steal near here.";
        public const string player_already_stealing = "You're already stealing items.";
        public const string player_not_stolen_items = "You don't have any stolen item.";
        public const string not_in_pawn_show = "You're not in a pawn shop.";
        public const string player_already_hotwiring = "You're already hotwiring a vehicle.";
        public const string player_stopped_hotwire = "You left the vehicle and stopped hotwiring it.";
        public const string engine_already_started = "This vehicle's engine is already toggled on.";
        public const string engine_on = "You have to toggle the vehicle engine off.";
        public const string vehicle_not_lockable = "This vehicle can't be locked.";
        public const string player_not_car = "You have to be in a car to use this animation.";
        public const string player_incriminated_himself = "You can't incriminate yourself.";
        public const string player_handcuffed_himself = "You can't handcuff yourself.";
        public const string player_fined_himself = "You can't fine yourself.";
        public const string player_is_dead = "You can't do that while dead.";
        public const string business_locked = "This business is locked.";
        public const string player_not_business_owner = "You're not the owner of this business.";
        public const string item_not_consumable = "You don't have any consumable in your hand.";
        public const string no_items_trunk = "This vehicle's trunk is empty.";
        public const string no_items_inventory = "You don't have any item in your inventory.";
        public const string no_items_near = "You don't have any item near.";
        public const string left_hand_empty = "You don't have any item in your left hand.";
        public const string right_hand_empty = "You don't have any item in your right hand.";
        public const string left_hand_occupied = "You already have an item in your left hand.";
        public const string right_hand_occupied = "You already have an item in your right hand.";
        public const string both_hand_occupied = "You already have an item in your hands.";
        public const string target_right_hand_not_empty = "The target player has already an item in his right hand.";
        public const string player_not_veh_owner = "You don-t have this vehicle's keys.";
        public const string not_in_route = "You're not in any job route.";
        public const string already_in_route = "You're in the middle of a job route.";
        public const string player_not_garbage = "You're not a garbage man.";
        public const string garbage_in_vehicle = "You can't collect garbage from the vehicle.";
        public const string not_garbage_near = "You don't have any garbage bag near.";
        public const string already_garbage = "You're already collecting garbage.";
        public const string player_not_in_news_van = "You have to be in the left rear seat of the news van.";
        public const string player_not_jail_area = "You're not in jail's entrance.";
        public const string player_searched_himself = "You can't frisk yourself.";
        public const string not_parking_near = "You're not in any parking.";
        public const string vehicle_faction_park = "You can't park this vehicle.";
        public const string parking_full = "This parking is full.";
        public const string player_not_garage_access = "You don't have this garage's keys.";
        public const string not_parking_allowed = "You can't park your vehicle here.";
        public const string vehicle_not_this_parking = "The vehicle is not parked here.";
        public const string not_in_vehicle_type_car = "You must be into a vehicle.";
        public const string hooker_offered_himself = "You can offer a service to yourself.";
        public const string no_service_offered = "Nobody offered you a sexual service.";
        public const string not_house_near = "There's no house near you.";
        public const string house_interior_modify = "The interior value must be between 0 and {0}.";
        public const string house_price_modify = "The price of the house must be greater than 0$.";
        public const string house_status_modify = "The state of this house must be between 0 and 2.";
        public const string house_rental_modify = "This house's rental price must be greater than 0$.";
        public const string player_not_enough_money = "You don't have enough money.";
        public const string mps_only_admin = "You can't use private messages with staff members.";
        public const string vehicle_not_exists = "Couldn't find any vehicle with that identifier.";
        public const string player_keys_full = "Player can't have more lent vehicles.";
        public const string player_not_in_right_rear = "You have to be on the back right rear seat of the van.";
        public const string no_walkie_in_hand = "You don't have any relay equipment in your hand.";
        public const string already_owned_channel = "You already have a radio channel created.";
        public const string not_owned_channel = "You don't own any radio channel.";
        public const string channel_not_found = "You couldn't connect to the radio frequency channel.";
        public const string no_blood_left = "There are no blood units remaining.";
        public const string player_not_on_duty = "You aren't on duty.";
        public const string player_not_on_admin_duty = "You aren't on duty as staff.";
        public const string business_clothes_not_available = "There are no clothes for sale.";
        public const string no_clothes_in_wardrobe = "There are no clothes in the wardrobe.";
        public const string player_not_hurt = "The target player is not wounded.";
        public const string no_telephone_hand = "You don't have any phone in your hand.";
        public const string player_not_called = "Nobody's calling at you in this moment.";
        public const string already_phone_talking = "You're already talking to the phone.";
        public const string not_phone_talking = "You're not in a phone call.";
        public const string faction_not_enough_money = "There are not enough funds into the faction's coffers.";
        public const string contact_list_empty = "You don't have any contact in your contact list.";
        public const string player_not_driving_school = "You're not in the driving school.";
        public const string vehicle_driving_not_suitable = "This vehicle isn't drivable with your license.";
        public const string not_testing_vehicle = "You're not allowed to test this vehicle.";
        public const string driving_school_money = "You need {0}$ to examinate.";
        public const string player_already_license = "Your license is still valid.";
        public const string player_not_townhall = "You're not in the town hall.";
        public const string on_air = "The target player is being interviewed already.";
        public const string not_on_air = "The target player isn't being interviewed.";
        public const string wheel_not_popped = "There's no need to repair this wheel.";
        public const string player_not_in_room_lockers = "You're not in the police station's lockers.";

        public const string player_not_enough_police_rank =
            "You don't have the required rank to get ammunition neither weapons.";

        public const string job_vehicle_abandoned = "Your delivered has been canceled for leaving your job vehicle.";
        public const string rank_too_low_recruit = "You don't have the required rank to recruit new members.";
        public const string rank_too_low_dismiss = "You don't have the required rank to dismiss members.";
        public const string rank_too_low_rank = "You don't have the required rank to change members' rank.";
        public const string player_already_faction = "The target player is already in a faction.";
        public const string player_not_in_same_faction = "The target player isn't in your faction.";
        public const string player_already_job = "The target player already has a job.";

        public const string player_undocumented =
            "You don't own any identification card, remember that it's mandatory to be identified.";

        public const string no_repair_offered = "Nobody offered you a repairment.";
        public const string no_repaint_offered = "Nobody offered you to repaint your vehicle.";

        public const string cant_toogle_engine_while_fucking =
            "You can't turn the engine on while having a sex service.";

        public const string cant_send_multiple_help_requests = "You have to wait before sending a new ticket.";
        public const string no_admins_on_duty_atm = "There are no staff members online, try again in a while.";
        public const string admin_ticket_not_found = "No ticket was found with that identifier.";
        public const string not_fuel_station_near = "You're not in any gas station.";
        public const string player_refueling = "You're already refueling your vehicle.";
        public const string vehicle_refueling = "The vehicle is being refueled.";

        public const string vehicle_start_refueling =
            "You can't turn the engine on while the vehicle is being refueled.";

        public const string vehicle_start_weapon_unpacking =
            "You can't turn the engine on while they're unpacking the weapons.";

        public const string vehicle_not_megaphone = "This vehicle doesn't have any megaphone installed.";
        public const string faction_warning_not_found = "Couldn't find the requested report.";
        public const string faction_warning_taken = "The selected report is being attended.";
        public const string player_have_faction_warning = "You're already attending a report.";
        public const string player_spectating = "The target player is spectating another player.";
        public const string cant_spect_self = "You can't spectate yourself.";
        public const string not_spectating = "You're not spectating any player.";
        public const string mask_equiped = "You're already wearing a mask.";
        public const string no_mask_equiped = "You're not wearing any mask.";
        public const string no_mask_bought = "You haven't purchased any mask.";
        public const string bag_equiped = "You're already wearing a bag.";
        public const string no_bag_equiped = "You're not wearing any bag.";
        public const string no_bag_bought = "You haven't purchased any bag.";
        public const string accessory_equiped = "You're already wearing an accessory.";
        public const string no_accessory_equiped = "You're not wearing any accessory.";
        public const string no_accessory_bought = "You haven't purchased any accessory.";
        public const string hat_equiped = "You're already wearing a hat.";
        public const string no_hat_equiped = "You're not wearing any hat.";
        public const string no_hat_bought = "You haven't purchased any hat.";
        public const string glasses_equiped = "You're already wearing a pair of glasses.";
        public const string no_glasses_equiped = "You're not wearing any glasses.";
        public const string no_glasses_bought = "You haven't purchased any glasses.";
        public const string ear_equiped = "You're already wearing a pair of earrings.";
        public const string no_ear_equiped = "You're not wearing any earrings.";
        public const string no_ear_bought = "You haven't purchased any earrings.";
        public const string player_not_thief_area = "You're not in the center of the city.";
        public const string stealing_progress = "You can't leave while you're stealing items.";
        public const string not_scrapyard_near = "You're not in the scrapyard.";
        public const string parking_not_garage = "You can't only link a house to a garage.";
        public const string not_house_business = "You're not in the door of any house or business.";
        public const string weapon_event_on_course = "The weapons event has already started.";
        public const string player_no_jerrycan = "You don't have any jerrycan in your hand.";
        public const string not_police_chief = "You're not the chief from the police department.";
        public const string player_cant_self = "You can't be the target player.";
        public const string not_valid_repair_place = "You're not in any workshop and don't have any towtruck near.";
        public const string wanted_vehicle_far = "The vehicle is too far away from you.";
        public const string not_required_products = "You need {0} products in order to do that.";
        public const string not_in_mechanic_workshop = "You're not in any workshop.";
        public const string employee_cooldown = "You need to get {0} more paydays to leave the job.";
        public const string player_inside_vehicle = "You can't do this inside a vehicle.";
        public const string haircut_money = "You need at least {0}$ to change your look.";
        public const string tattoo_duplicated = "You already have a tattoo like this one.";
        public const string vehicle_trunk_opened = "The vehicle's trunk is already opened.";
        public const string vehicle_trunk_closed = "The vehicle's trunk is already closed.";
        public const string vehicle_trunk_in_use = "There's already somebody interacting with this vehicle's trunk.";
        public const string parking_not_money = "You need {0}$ to get your car from the parking.";
        public const string player_not_rod_boat = "You don't have any fishing rod and you're not in any fishing boat.";
        public const string not_fishing_boat = "This vehicle isn't a fishing boat.";
        public const string player_not_fisherman = "You're not fisherman.";
        public const string not_fishing_zone = "You're not in a fishing area.";
        public const string no_fishing_rod = "You don't have a fishing rod in your right hand.";
        public const string player_no_baits = "You have nothing to use as a bait.";
        public const string not_fishing_business = "You're not in a fishing store.";
        public const string fishing_canceled = "You reeled too early.";
        public const string fishing_failed = "The fish has escaped from the hook.";
        public const string player_already_fishing = "You're already fishing.";
        public const string no_fish_sellable = "You don't have any fish to sell.";
        public const string weather_value_invalid = "Weather value must be in 0 to 12 interval.";
        public const string bank_not_enough_money = "The bank account has not enough funds to process the operation.";
        public const string transfer_money_own = "You can't transfer money to your own account.";
        public const string bank_account_not_found = "There's no bank account linked to that person.";
        public const string bank_general_error = "An error occurred while processing the operation.";
        public const string low_blood = "Your blood level is to low to be drawn.";
        public const string player_not_taxi_license = "You don't own any taxi driving license.";
        public const string no_taxi_driver = "Nobody is driving this taxi.";
        public const string taxi_has_path = "The taxi driver already has a destination point.";
        public const string not_trucker = "You're not a trucker.";
    }
}