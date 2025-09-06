using Systems.SimpleCore.Identifiers;

namespace Systems.SimpleCore.Storage
{
    public struct AddressableDatabaseEntry<TEntryObject>
    {
        public readonly HashIdentifier hashIdentifier;
        public readonly TEntryObject entryObject;

        public AddressableDatabaseEntry(HashIdentifier hashIdentifier, TEntryObject entryObject)
        {
            this.hashIdentifier = hashIdentifier;
            this.entryObject = entryObject;
        }
    }
}