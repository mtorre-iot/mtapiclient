namespace gaihcc2dataserver.classes;
public class CircularBuffer<T>
{
    private readonly T[] buffer;
    private int head;
    private int tail;
    private bool isFull;
    private readonly int capacity;
    private readonly object syncLock = new object();
    public string topic { get; set; }
    public string channelName { get; set; }

    public CircularBuffer(string topic, string channelName, int capacity)
    {
        this.topic = topic;
        this.channelName = channelName;
        this.capacity = capacity;
        buffer = new T[capacity];
        head = 0;
        tail = 0;
        isFull = false;
    }

    private void Enqueue(T item)
    {
        buffer[tail] = item;
        tail = (tail + 1) % capacity;

        if (isFull)
        {
            head = (head + 1) % capacity; // Overwrite oldest
        }

        isFull = tail == head;
    }

    public void Enqueue(T[] items, int interleave)
    {
        lock (syncLock)
        {
            if (interleave < 0)
            {
                interleave = 1;
            }
            if (interleave >= items.Length)
            {
                interleave = items.Length;
            }
            if (items.Length % interleave != 0)
            {
                throw new Exception($"CircularBuffer - Interleave value: {interleave} is not valid for block size: {items.Length}");
            }
            for (int i = 0; i < items.Length; i += interleave)
            {
                Enqueue(items[i]);
            }
        }
    }

    private T Dequeue()
    {

        if (IsEmpty)
            throw new InvalidOperationException("Buffer is empty.");

        T item = buffer[head];
        head = (head + 1) % capacity;
        isFull = false;
        return item;
    }

    public T[] Dequeue(int block_size)
    {
        T[] rtn = new T[block_size];

        lock (syncLock)
            {
            if (Count < block_size)
            {
                return null;
            }

            int i = 0;
            for (; i < block_size; i++)
            {
                try
                {
                    T item = Dequeue();
                    rtn[i] = item;
                }
                catch
                {
                    break;
                }
            }
            if (i < block_size)
            {
                return null;
            }
        }
        return rtn;
    }

    public bool IsEmpty
    {
        get
        {
            lock (syncLock)
            {
                return !isFull && head == tail;
            }
        }
    }

    public bool IsFull
    {
        get
        {
            lock (syncLock)
            {
                return isFull;
            }
        }
    }

    public int Count
    {
        get
        {
            lock (syncLock)
            {
                if (isFull)
                    return capacity;
                if (tail >= head)
                    return tail - head;
                return capacity - head + tail;
            }
        }
    }

    public void PrintBuffer()
    {
        lock (syncLock)
        {
            Console.Write("Buffer contents: ");
            int count = Count;
            for (int i = 0; i < count; i++)
            {
                int index = (head + i) % capacity;
                Console.Write(buffer[index] + " ");
            }
            Console.WriteLine();
        }
    }
}
