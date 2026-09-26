// using System;

// public static class SuitsHandler
// {
//     enum SuitName
//     {
//         Bat_Suit,
//         Heat_Suit,
//         Glide_Suit,
//         Demo_Suit,
//         Sonar_Suit,

//         Robin_Suit,
//         Dive_Suit,
//         Tech_Suit,
//         Magnet_Suit,
//         Attracto_Suit,

//         Blocked_Suit
//     };

//     enum SuitID
//     {
//         /*
//         7608:batman suit
//         7658:batman heat suit
//         76A8:batman glide suit
//         76F8:batman demo suit
//         7748:batman sonar suit
//         7798:robin suit
//         77E8:robin dive suit
//         7838:robin tech suit
//         7888:robin magnet suit
//         78D8:robin attracto suit

//         D628: nothing (no wear)

//         1000:batman secret invis suit
//         5608:batman cowl only
//         C6A8:batman cowl and cape (no wear)
//         */
//         Bat_SuitID = 0x7608,
//         Heat_SuitID = 0x7658,
//         Glide_SuitID = 0x76A8,
//         Demo_SuitID = 0x76F8,
//         Sonar_SuitID = 0x7748,
//         Robin_SuitID = 0x7798,
//         Dive_SuitID = 0x77E8,
//         Tech_SuitID = 0x7838,
//         Magnet_SuitID = 0x7888,
//         Attracto_SuitID = 0x78D8,
//         Blocked_SuitID = 0xD628 // this likely points to something that coincidentally makes a suit that can't be worn.
//     };

//     SuitName SuitIDtoSuitName(WORD id)
//     {
//         return SuitIDtoSuitName(static_cast<SuitID>(id));
//     }

//     SuitName SuitIDtoSuitName(SuitID id)
//     {
//         switch (id)
//         {
//             case Bat_SuitID: return Bat_Suit;
//             case Heat_SuitID: return Heat_Suit;
//             case Glide_SuitID: return Glide_Suit;
//             case Demo_SuitID: return Demo_Suit;
//             case Sonar_SuitID: return Sonar_Suit;

//             case Robin_SuitID: return Robin_Suit;
//             case Dive_SuitID: return Dive_Suit;
//             case Tech_SuitID: return Tech_Suit;
//             case Magnet_SuitID: return Magnet_Suit;
//             case Attracto_SuitID: return Attracto_Suit;

//             default: return Blocked_Suit;
//         }
//     }

//     SuitID SuitNametoSuitID(size_t name)
//     {
//         return SuitNametoSuitID(static_cast<SuitName>(name));
//     }

//     SuitID SuitNametoSuitID(SuitName name)
//     {
//         switch (name)
//         {
//             case Bat_Suit: return Bat_SuitID;
//             case Heat_Suit: return Heat_SuitID;
//             case Glide_Suit: return Glide_SuitID;
//             case Demo_Suit: return Demo_SuitID;
//             case Sonar_Suit: return Sonar_SuitID;

//             case Robin_Suit: return Robin_SuitID;
//             case Dive_Suit: return Dive_SuitID;
//             case Tech_Suit: return Tech_SuitID;
//             case Magnet_Suit: return Magnet_SuitID;
//             case Attracto_Suit: return Attracto_SuitID;

//             default: return Blocked_SuitID;
//         }
//     }


//     Suits::Suits(DWORD BASE_ADDR): 
//     BASE_ADDR(BASE_ADDR),
//     _unlocked1(*reinterpret_cast<volatile WORD*>(BASE_ADDR + 0x006C9450)),
//     _unlocked2(
//         *reinterpret_cast<volatile WORD*>(
//             reinterpret_cast<uintptr_t>(
//                 * reinterpret_cast<void**>(BASE_ADDR + 0x005C49CC)
//             ) + 0x7BB4
//         )
//     ),
//     _signals(BASE_ADDR)
//     {
//         /* signals
//         *(LEGOBatman.exe+6BDF84)+8C
//         *(LEGOBatman.exe+6BDF84)+8C+3D8
//         *(LEGOBatman.exe+6BDF84)+8C+3D8+3D8
//         *(LEGOBatman.exe+6B8BBC)+8C
//         *(LEGOBatman.exe+6B8BBC)+8C+3D8
//         *(LEGOBatman.exe+6B8BBC)+8C+3D8+3D8
//         */
//         for (size_t i = 0; i < 6; i++)
//         {
//             _signalsStart[i] = 0;
//             _signalsPrev[i] = 0;
//         }

//     }

//     public void lock(uint i)
//     {
//         // untested
//         _unlocked1 &= !((uint)(1 << i));
//         _unlocked2 &= !((uint)(1 << i));
//     }

//     void unlock(size_t i)
//     {
//         _unlocked1 |= (WORD)(1 << i);
//         _unlocked2 |= (WORD)(1 << i);
//     }

//     void updateSuits()
//     {
//         for (size_t i = 0; i < 10; i++)
//         {
//             if (lb1AP_items[80 + i])
//             {
//                 unlock(i);
//             }
//             else
//             {
//                 // untested
//                 lock (i) ;
//             }
//         }

//     }
// }
