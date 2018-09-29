using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace CBC.Neo.SmartContract
{
    public class CBC : Framework.SmartContract
    {
        //Token Settings
        public static string Name() => "Community Based Creation";
        public static string Symbol() => "CBC";
        public static readonly byte[] Owner = "APuZgU3BCQxEjb4jUETDcsgZmErDW97k34".ToScriptHash();
        public static byte Decimals() => 8;
        private const ulong factor = 100000000; //decided by Decimals()
        private const ulong total_amount = 1000000000 * factor; // total token amount
        

        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> Transferred;

        [DisplayName("approval")]
        public static event Action<byte[], byte[], BigInteger> Approval;

        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                if (Owner.Length == 20)
                {
                    // if param Owner is script hash
                    return Runtime.CheckWitness(Owner);
                }
                else if (Owner.Length == 33)
                {
                    // if param Owner is public key
                    byte[] signature = operation.AsByteArray();
                    return VerifySignature(signature, Owner);
                }
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                if (operation == "deploy") return Deploy();
                if (operation == "totalSupply") return TotalSupply();
                if (operation == "name") return Name();
                if (operation == "symbol") return Symbol();
                if (operation == "decimals") return Decimals();
                if (operation == "transfer")
                {
                    if (args.Length != 3) return false;
                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    BigInteger value = (BigInteger)args[2];
                    return Transfer(from, to, value);
                }
                if (operation == "balanceOf")
                {
                    if (args.Length != 1) return 0;
                    byte[] account = (byte[])args[0];
                    return BalanceOf(account);
                }
				if (operation == "transferFrom")
                {
                    if (args.Length != 4) return false;
					byte[] originator = (byte[])args[0];
                    byte[] from = (byte[])args[1];
                    byte[] to = (byte[])args[2];
                    BigInteger value = (BigInteger)args[3];
                    return TransferFrom(originator, from, to, value);
                }
                if (operation == "approve")
                {
                    if (args.Length != 3) return false;
                    byte[] originator = (byte[])args[0];
                    byte[] spender = (byte[])args[1];
                    BigInteger value = (BigInteger)args[2];
                    return Approve(originator, spender, value);
                }
                if (operation == "allowance")
                {
                    if (args.Length != 2) return false;
                    byte[] originator = (byte[])args[0];
                    byte[] spender = (byte[])args[1];
                    return Allowance(originator, spender);
                }
            }
            return false;
        }

        // initialization parameters, only once
        public static bool Deploy()
        {
            byte[] total_supply = Storage.Get(Storage.CurrentContext, "totalSupply");
            if (total_supply.Length != 0) return false;
            Storage.Put(Storage.CurrentContext, Owner, total_amount);
            Storage.Put(Storage.CurrentContext, "totalSupply", total_amount);
            Transferred(null, Owner, total_amount);
            return true;
        }

        // get the total token supply
        public static BigInteger TotalSupply()
        {
            return Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
        }

        // function that is always called when someone wants to transfer tokens.
        public static bool Transfer(byte[] from, byte[] to, BigInteger value)
        {
            if (value <= 0) return false;
            if (!Runtime.CheckWitness(from)) return false;
            if (from == to) return true;
            BigInteger from_value = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
            if (from_value < value) return false;
            if (from_value == value)
                Storage.Delete(Storage.CurrentContext, from);
            else
                Storage.Put(Storage.CurrentContext, from, from_value - value);
            BigInteger to_value = Storage.Get(Storage.CurrentContext, to).AsBigInteger();
            Storage.Put(Storage.CurrentContext, to, to_value + value);
            Transferred(from, to, value);
            return true;
        }

        // get the account balance of another account with address
        public static BigInteger BalanceOf(byte[] address)
        {
            return Storage.Get(Storage.CurrentContext, address).AsBigInteger();
        }

		// Transfers a balance from one account to another on behalf of the account owner.
        public static bool TransferFrom(byte[] originator, byte[] from, byte[] to, BigInteger value)
        {
			if (!Runtime.CheckWitness(originator)) return false;
			
            BigInteger allValInt = Storage.Get(Storage.CurrentContext, from.Concat(originator)).AsBigInteger();
            BigInteger fromValInt = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
            BigInteger toValInt = Storage.Get(Storage.CurrentContext, to).AsBigInteger();
 
            if (fromValInt >= value &&
                value > 0  &&
                allValInt >= value)
            {
                Storage.Put(Storage.CurrentContext, from.Concat(originator), allValInt - value);
                Storage.Put(Storage.CurrentContext, to, toValInt + value);
                Storage.Put(Storage.CurrentContext, from, fromValInt - value);
                Transferred(from, to, value);
                return true;
            }
            return false;
        }

        // Approves another user to use the TransferFrom function on the invoker's account.
        public static bool Approve(byte[] originator, byte[] spender, BigInteger value)
        {
			if (!Runtime.CheckWitness(originator)) return false;
			
            Storage.Put(Storage.CurrentContext, originator.Concat(spender), value);
			Approval(originator, spender, value);
            return true;
        }
        
        // Checks the TransferFrom approval of two accounts.
        public static BigInteger Allowance(byte[] originator, byte[] spender)
        {
            return Storage.Get(Storage.CurrentContext, originator.Concat(spender)).AsBigInteger();
        }
    }
}