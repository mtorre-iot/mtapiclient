using System.Runtime.Intrinsics.X86;

namespace mtapiclient.classes;

public class GAIBuffer<T>
{
    public string topic { get; set; }
    public string channelName { get; set; }
    private object _lock = new object();
    private T[] _gaibuffer { get; set; }
    private int _head;
    private int _tail;
    private int _size { get; set; }
    public int capacity;
    public int count => _size;
    public bool isFull => _size == capacity;
    public bool isEmpty => _size == 0;
    public GAIBuffer(string topic, string channelName, int capacity)
    {
        this.topic = topic;
        this.channelName = channelName;
        this._gaibuffer = new T[capacity];
        this.capacity = capacity;
        _head = 0;
        _tail = 0;
        _size = 0;
    }
    public T Egress()
    {
        lock (_lock)
        {
            if (isEmpty == true)
            {
                throw new Exception("GAIbuffer - Buffer is empty.");
            }
            T item = _gaibuffer[_head];
            _head = (_head + 1) % capacity;
            _size--;
            return item;
        }
    }

    public T[] Egress(int block_size)
    {
        lock (_lock)
        {
            if (isEmpty == true)
            {
                throw new Exception("GAIbuffer - Buffer is empty.");
            }
            T[] rtn = new T[block_size];

            if (block_size > _size)
            {
                block_size = _size;
            }
            for (int i = 0; i < block_size; i++)
            {
                T item = _gaibuffer[_head];
                rtn[i] = item;
                _head = (_head + 1) % capacity;
                _size--;
            }
            return rtn;
        }
    }
    public void Ingress(T item)
    {
        lock (_lock)
        {
            if (isFull == true)
            {
                throw new Exception("GAIBuffer - Buffer is full.");
            }
            _gaibuffer[_tail] = item;
            _tail = (_tail + 1) % capacity;
            _size++;
        }
    }

    public void Ingress(T[] item_list, int interleave = 1)
    {
        if (interleave < 0)
        {
            interleave = 1;
        }
        if (interleave >= item_list.Length)
        {
            interleave = item_list.Length;
        }
        if (item_list.Length % interleave != 0)
        {
            throw new Exception($"GAIBuffer - Interleave value: {interleave} is not valid for block size: {item_list.Length}");
        }
        lock (_lock)
        {
            for (int i = 0; i < item_list.Length; i = i + interleave)
            {
                if (isFull == true)
                {
                    throw new Exception("GAIBuffer - Buffer is full.");
                }
                _gaibuffer[_tail] = item_list[i];
                _tail = (_tail + 1) % capacity;
                _size++;
            }
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _head = 0;
            _tail = 0;
            _size = 0;
        }
    }

    public int GetSize() => _size;
}
