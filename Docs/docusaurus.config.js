// @ts-check
/** @type {import('@docusaurus/types').Config} */
const config = {
  title: 'Unity MCP Server',
  tagline: 'Control Unity with AI',
  url: 'https://your-site.com',
  baseUrl: '/',
  onBrokenLinks: 'throw',
  markdown: {
    format: 'mdx',
    mermaid: true,
    preprocessor: ({filePath, fileContent}) => {
      return fileContent;
    },
    mdx1Compat: {
      comments: true,
      admonitions: true,
      headingIds: true,
    },
  },
  favicon: 'img/favicon.ico',
  organizationName: 'your-org', // Usually your GitHub org/user name.
  projectName: 'unity-mcp', // Usually your repo name.

  presets: [
    [
      'classic',
      /** @type {import('@docusaurus/preset-classic').Options} */
      ({
        docs: {
          routeBasePath: '/',
          sidebarPath: require.resolve('./sidebars.js'),
          editUrl: 'https://github.com/your-org/unity-mcp/tree/main/Docs/',
        },
        theme: {
          customCss: require.resolve('./src/css/custom.css'),
        },
      }),
    ],
  ],

  themeConfig:
    /** @type {import('@docusaurus/preset-classic').ThemeConfig} */
    ({
      navbar: {
        title: 'Unity MCP',
        items: [
          {
            type: 'docSidebar',
            sidebarId: 'tutorialSidebar',
            position: 'left',
            label: 'Docs',
          },
          {
            href: 'https://github.com/your-org/unity-mcp',
            label: 'GitHub',
            position: 'right',
          },
        ],
      },
      footer: {
        style: 'dark',
        links: [
          {
            title: 'Docs',
            items: [
              {
                label: 'Introduction',
                to: '/docs/intro',
              },
              {
                label: 'RPC API reference',
                to: '/docs/rpc/overview',
              },
            ],
          },
          {
            title: 'Community',
            items: [
              {
                label: 'GitHub',
                href: 'https://github.com/your-org/unity-mcp',
              },
            ],
          },
        ],
        copyright: `Copyright © ${new Date().getFullYear()} Mitch Christow. Built with Docusaurus.`,
      },
    }),
};

module.exports = config;
