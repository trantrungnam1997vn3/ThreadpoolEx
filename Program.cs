using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using TestThreadpool.Models;

namespace TestThreadpool
{

    class Program
    {
        static int inputs = 100;

        public List<Product> Products = new List<Product>();

        public static List<Product> InitialProducts()
        {
            List<Product> products = new List<Product>();
            for (int i = 0; i < inputs; i++)
            {
                products.Add(new Product
                {
                    Id = i,
                    Name = "Item" + i,
                    Email = "bot" + i + "@gmail.com"
                });

            }
            return products;
        }


        class TryTakeDemo
        {

            // Demonstrates:
            //      BlockingCollection<T>.Add()
            //      BlockingCollection<T>.CompleteAdding()
            //      BlockingCollection<T>.TryTake()
            //      BlockingCollection<T>.IsCompleted
            public static void BC_TryTake()
            {
                List<Product> products = InitialProducts();
                // Construct and fill our BlockingCollection
                using (BlockingCollection<Product> bc = new BlockingCollection<Product>())
                {
                    for (int i = 0; i < products.Count; i++)
                    {
                        bc.Add(products[i]);
                    }
                    bc.CompleteAdding();
                    int outersum = 0;

                    //Delegate for consuming the BlockingCollection and adding up all items

                    Action action = () =>
                    {
                        using (var context = new ProductContext())
                        {
                            Product localItem;
                            Console.WriteLine("Thread: {0}", Thread.CurrentThread.ManagedThreadId);
                            while (!bc.IsCompleted)
                            {
                                try
                                {
                                    if (!bc.TryTake(out localItem, 0))
                                    {
                                        Console.WriteLine(" Take Blocked");
                                    }
                                    else
                                    {
                                        Console.WriteLine(" Take:{0}, Thread: {1}", localItem.Email, Thread.CurrentThread.ManagedThreadId);
                                        context.Products.Add(localItem);
                                        context.SaveChanges();
                                    }
                                }

                                catch (OperationCanceledException)
                                {
                                    Console.WriteLine("Taking canceled.");
                                    break;
                                }
                            }
                            Console.WriteLine("\r\n Success");
                            Console.WriteLine("\r\nNo more items to take.");
                        }
                    };
                    // Launch three parallel actions to consume the BlockingCollection
                    Parallel.Invoke(action, action, action);
                    Console.WriteLine("bc.IsCompleted = {0} (should be true)", bc.IsCompleted);
                }
            }
        }

        public static async Task ProgressWithAsync()
        {
            using (BlockingCollection<Product> bc = new BlockingCollection<Product>())
            {
                List<Product> products = InitialProducts();
                // Spin up a Task to populate the BlockingCollection
                using (Task t1 = Task.Run(() =>
                {
                    for (int i = 0; i < products.Count; i++)
                    {
                        bc.Add(products[i]);
                    }
                    bc.CompleteAdding();
                }))
                {
                    // Spin up a Task to consume the BlockingCollection
                    using (Task t2 = Task.Run(() =>
                    {
                        Action action = () =>
                        {
                            using (var context = new ProductContext())
                            {
                                Product localItem;
                                while (!bc.IsCompleted)
                                {
                                    try
                                    {
                                        if (bc.TryTake(out localItem))
                                        {
                                            // Consume consume the BlockingCollection
                                            context.Products.Add(localItem);
                                            context.SaveChanges();
                                            Console.WriteLine("Data :{0}, Thread Id: {1}", localItem.Email,
                                                Thread.CurrentThread.ManagedThreadId);
                                        }
                                        else
                                        {
                                            Console.WriteLine("Take blocked");
                                        }

                                    }
                                    catch (InvalidOperationException)
                                    {
                                        // An InvalidOperationException means that Take() was called on a completed collection
                                        Console.WriteLine("That's All!");
                                    }
                                }
                            }
                        };

                        Parallel.Invoke(action, action);
                    }))
                    {
                        await Task.WhenAll(t1, t2);
                    }
                }
            }
        }

        static void Main()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            //TryTakeDemo.BC_TryTake();
            ProgressWithAsync().GetAwaiter().GetResult();
            stopwatch.Stop();
            Console.WriteLine("Time consuming {0}", stopwatch.Elapsed);
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        //static void Main()
        //{
        //    List<Product> products = InitialProducts();
        //    Stopwatch stopwatch = new Stopwatch();
        //    stopwatch.Start();
        //    CancellationTokenSource cts = new CancellationTokenSource();
        //    BlockingCollection<int> numberCollection = new BlockingCollection<int>(20);
        //    Task.Run(() =>
        //    {
        //        if (Console.ReadKey(true).KeyChar == 'c')
        //            cts.Cancel();
        //    });

        //    Task t1 = Task.Run(() => NonBlockingConsumer(numberCollection, cts.Token));
        //    Task t2 = Task.Run(() => NonBlockingProducer(numberCollection, cts.Token, products));
        //    Task.WaitAll(t1, t2);

        //    stopwatch.Stop();
        //    Console.WriteLine("Time comsumed {0}", stopwatch.Elapsed);

        //    cts.Dispose();
        //    Console.WriteLine("Press the Enter key to exit.");
        //    Console.ReadLine();
        //}

        //static void NonBlockingConsumer(BlockingCollection<int> bc, CancellationToken ct)
        //{
        //    while (!bc.IsCompleted)
        //    {
        //        int nextItem;
        //        try
        //        {
        //            if (!bc.TryTake(out nextItem, 0, ct))
        //            {
        //                Console.WriteLine(" Take Blocked");
        //            }
        //            else
        //            {
        //                Console.WriteLine(" Take:{0}", nextItem);
        //            }
        //        }

        //        catch (OperationCanceledException)
        //        {
        //            Console.WriteLine("Taking canceled.");
        //            break;
        //        }
        //        //Thread.SpinWait(5000000);
        //    }
        //    Console.WriteLine("\r\n Success");
        //    Console.WriteLine("\r\nNo more items to take.");
        //}

        //static void NonBlockingProducer(BlockingCollection<int> bc, CancellationToken ct, List<Product> products)
        //{
        //    int itemToAdd = 0;
        //    bool success = false;
        //    do
        //    {
        //        try
        //        {
        //            success = bc.TryAdd(itemToAdd, 2, ct);
        //        }
        //        catch (OperationCanceledException)
        //        {
        //            Console.WriteLine("Add loop canceled.");
        //            bc.CompleteAdding();
        //            break;
        //        }

        //        if (success)
        //        {
        //            AddNewItem(products[itemToAdd]);
        //            Console.WriteLine(" Add:{0}, Total: {1}, Thread: {2}", itemToAdd, bc.Count, Thread.CurrentThread.ManagedThreadId);
        //            itemToAdd++;
        //        }
        //        else
        //        {
        //            Console.Write(" AddBlocked:{0} Count = {1}", itemToAdd.ToString(), bc.Count);
        //            UpdateProgress(itemToAdd);
        //        }

        //    } while (itemToAdd < products.Count);
        //    bc.CompleteAdding();
        //}

        static int AddNewItem(Product product)
        {
            using (var context = new ProductContext())
            {
                Console.WriteLine("Go" + product.Email);
                context.Products.Add(product);
                context.SaveChanges();
            }
            return product.Id;
        }

        static void UpdateProgress(int i)
        {
            double percent = ((double)i / inputs) * 100;
            Console.WriteLine("Percent complete: {0}", percent);
        }

        static void UpdateCurrentProgress(int i, int total)
        {
            double percent = ((double)i / total) * 100;
            Console.WriteLine("Percent complete: {0}", percent);
        }
    }

}
