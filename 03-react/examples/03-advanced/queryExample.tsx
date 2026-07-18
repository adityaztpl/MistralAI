import {
  QueryClient,
  QueryClientProvider,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query';

type Product = {
  id: string;
  name: string;
  price: number;
};

type ProductDraft = {
  name: string;
  price: number;
};

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 60_000,
      retry: 1,
    },
  },
});

async function fetchProducts(): Promise<Product[]> {
  const response = await fetch('/api/products');

  if (!response.ok) {
    throw new Error(`Failed to fetch products: ${response.status}`);
  }

  return (await response.json()) as Product[];
}

async function createProduct(draft: ProductDraft): Promise<Product> {
  const response = await fetch('/api/products', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(draft),
  });

  if (!response.ok) {
    throw new Error(`Failed to create product: ${response.status}`);
  }

  return (await response.json()) as Product;
}

function Products() {
  const queryClient = useQueryClient();
  const productsQuery = useQuery({
    queryKey: ['products'],
    queryFn: fetchProducts,
  });

  const createProductMutation = useMutation({
    mutationFn: createProduct,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['products'] });
    },
  });

  if (productsQuery.isPending) {
    return <p>Loading products...</p>;
  }

  if (productsQuery.isError) {
    return <p role="alert">{productsQuery.error.message}</p>;
  }

  return (
    <section>
      <h2>Products</h2>
      <button
        type="button"
        disabled={createProductMutation.isPending}
        onClick={() =>
          createProductMutation.mutate({
            name: 'Interview Prep Notebook',
            price: 19,
          })
        }
      >
        {createProductMutation.isPending ? 'Creating...' : 'Create sample product'}
      </button>

      <ul>
        {productsQuery.data.map((product) => (
          <li key={product.id}>
            {product.name} - ${product.price}
          </li>
        ))}
      </ul>
    </section>
  );
}

export function QueryExampleApp() {
  return (
    <QueryClientProvider client={queryClient}>
      <Products />
    </QueryClientProvider>
  );
}
