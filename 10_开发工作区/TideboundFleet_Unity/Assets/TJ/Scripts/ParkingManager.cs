using System.Collections.Generic;
using UnityEngine;

namespace TJ.Scripts
{
    public class ParkingManager : Singleton<ParkingManager>
    {
        public List<ParkingSlots> slots;
        public List<ParkingSlots> lockedSlots;
        public List<Vehicle> parkedVehicles;
        public ParkingSlots parkingSlot_Rv;

        public Transform exitPoint;
        public ParkingSlots CheckForFreeSlot()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].isOccupied)
                {
                    return slots[i];
                }
            }
            return null;
        }
        public void UnlockSlot()
        {
            for(int i = 0; i < lockedSlots.Count; i++)
            {
                if (!slots.Contains(lockedSlots[i]))
                {
                    lockedSlots[i].UnlockSlot_Callback();
                    lockedSlots.RemoveAt(i);
                    break;
                }
            }
        }
    }
}