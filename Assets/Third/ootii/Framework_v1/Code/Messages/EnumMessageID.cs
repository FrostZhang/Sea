using UnityEngine;
using System.Collections;

namespace com.ootii.Messages
{
    /// <summary>
    /// Provides a centralized list for common messages
    /// </summary>
    public partial class EnumMessageID
    {
        public static int MSG_UNKNOWN = 0;

        // Navigation values
        public static int MSG_NAVIGATE_ARRIVED = 1;
        public static int MSG_NAVIGATE_SLOW_ENTERED = 2;
        public static int MSG_NAVIGATE_WALK = 5;
        public static int MSG_NAVIGATE_JUMP = 10;
        public static int MSG_NAVIGATE_CLIMB = 15;
        public static int MSG_NAVIGATE_PUSHED_BACK = 20;
        public static int MSG_NAVIGATE_KNOCKED_DOWN = 25;

        // Motion valuees
        public static int MSG_MOTION_UNKNOWN = 100;
        public static int MSG_MOTION_ACTIVATE = 101;
        public static int MSG_MOTION_CONTINUE = 102;
        public static int MSG_MOTION_DEACTIVATE = 103;
        public static int MSG_MOTION_TEST = 104;

        // Camera values
        public static int MSG_CAMERA_MOTOR_UNKNOWN = 200;
        public static int MSG_CAMERA_MOTOR_ACTIVATE = 201;
        public static int MSG_CAMERA_MOTOR_DEACTIVATE = 202;
        public static int MSG_CAMERA_MOTOR_TEST = 203;

        // Interaction values
        public static int MSG_INTERACTION_ACTIVATE = 300;

        // Combat values
        public static int MSG_COMBAT_UNKNOWN = 1000;
        public static int MSG_COMBAT_COMBATANT_CANCEL = 1001;
        public static int MSG_COMBAT_COMBATANT_ATTACK = 1002;
        public static int MSG_COMBAT_COMBATANT_BLOCK = 1003;
        public static int MSG_COMBAT_COMBATANT_PARRY = 1004;
        public static int MSG_COMBAT_COMBATANT_EVADE = 1005;
        public static int MSG_COMBAT_ATTACKER_PRE_ATTACK = 1100;
        public static int MSG_COMBAT_ATTACKER_ATTACKED = 1101;
        public static int MSG_COMBAT_DEFENDER_ATTACKED = 1150;
        public static int MSG_COMBAT_DEFENDER_ATTACKED_IGNORED = 1102;
        public static int MSG_COMBAT_DEFENDER_ATTACKED_BLOCKED = 1103;
        public static int MSG_COMBAT_DEFENDER_ATTACKED_PARRIED = 1104;
        public static int MSG_COMBAT_DEFENDER_ATTACKED_EVADED = 1105;
        public static int MSG_COMBAT_DEFENDER_DAMAGED = 1107;
        public static int MSG_COMBAT_DEFENDER_KILLED = 1108;
        public static int MSG_COMBAT_ATTACKER_POST_ATTACK = 1149;
        public static int MSG_COMBAT_ATTACKER_TARGET_LOCKED = 1150;
        public static int MSG_COMBAT_ATTACKER_TARGET_UNLOCKED = 1151;

        // Inventory values
        public static int MSG_INVENTORY_UNKNOWN = 1500;
        public static int MSG_INVENTORY_ITEM_EQUIPPED = 1501;
        public static int MSG_INVENTORY_ITEM_STORED = 1502;
        public static int MSG_INVENTORY_WEAPON_SET_EQUIPPED = 1503;
        public static int MSG_INVENTORY_WEAPON_SET_STORED = 1504;

        // Magic values
        public static int MSG_MAGIC_UNKNOWN = 5000;
        public static int MSG_MAGIC_CAST = 5001;
        public static int MSG_MAGIC_CONTINUE = 5002;
        public static int MSG_MAGIC_CANCEL = 5003;
        public static int MSG_MAGIC_PRE_CAST = 5004;
        public static int MSG_MAGIC_POST_CAST = 5005;

        // Sensor values
        public static int MSG_SENSORS_OBJECTS_DETECTED_ENTER = 5100;
        public static int MSG_SENSORS_OBJECTS_DETECTED_STAY = 5101;
        public static int MSG_SENSORS_OBJECTS_DETECTED_EXIT = 5102;

        // Attribute values
        public static int MSG_ATTRIBUTES_VALUE_CHANGED = 5200;

        // Faction values
        public static int MSG_FACTIONS_VALUE_CHANGED = 5300;

        // Memory values
        public static int MSG_MEMORIES_MEMORY_EXPIRED = 5400;
    }
}
