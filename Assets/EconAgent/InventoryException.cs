using System;

[Serializable]
    public class InventoryException : Exception
    {
        public InventoryException(string message) : base(message)
        {
        }
    }
